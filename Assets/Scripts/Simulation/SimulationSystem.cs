using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using Game.GameElements.Runtime;

namespace Game.Simulation
{

class SimulationCmdBuffer : EntityCommandBufferSystem { }

public class SimulationSystem : JobComponentSystem
{
	// PRIVATES FIELDS

	private SimulationCmdBuffer cmdBuffer;

	private EntityQuery levelQuery;
	private LevelInfo level;
	private Entity levelEntity;

	private EntityQuery swapQueriesQuery;
	private NativeHashMap<int, float2> patternMatchRequest;

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		cmdBuffer = World.GetOrCreateSystem<SimulationCmdBuffer>();

		levelQuery = GetEntityQuery(
			ComponentType.ReadWrite<LevelInfo>()
		);

		swapQueriesQuery = GetEntityQuery(
			ComponentType.ReadOnly<SwapQuery>()
		);
	}

	protected override void OnStartRunning()
	{
		patternMatchRequest = new NativeHashMap<int, float2>(10, Allocator.Persistent);
		GetLevelInfo();
	}

	protected override void OnStopRunning()
	{
		patternMatchRequest.Dispose();
	}

	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		if(patternMatchRequest.IsCreated)
		{
			patternMatchRequest.Clear();
		}

		var swapTestJob = new SwapTest() {
			level     = EntityManager.GetBuffer<Level>(levelEntity),
			levelInfo = level,
			cmdBuff   = cmdBuffer.CreateCommandBuffer().ToConcurrent(),
			patternMatchRequest = patternMatchRequest.ToConcurrent()
		};

		var patternMatching = new PatternMatching() {
			patternsBufferLookup = GetBufferFromEntity<Pattern>(isReadOnly: true),
			patternMatchRequest  = patternMatchRequest,
			levelBufferLookup    = GetBufferFromEntity<Level>(isReadOnly: true),
			levelInfo            = level
		};

		jobs = swapTestJob.Schedule(this, jobs);
		jobs = patternMatching.Schedule(this, jobs);

		return jobs;
	}

	// PRIVATES METHODS

	private void GetLevelInfo()
	{
		var temp_level       = levelQuery.ToComponentDataArray<LevelInfo>(Allocator.TempJob);
		var temp_levelEntity = levelQuery.ToEntityArray(Allocator.TempJob);

		level       = temp_level[0];
		levelEntity = temp_levelEntity[0];

		temp_level.Dispose();
		temp_levelEntity.Dispose();
	}

	// JOBS

	struct SwapTest : IJobForEachWithEntity<SwapQuery>
	{
		[ReadOnly]
		public DynamicBuffer<Level> level;

		[ReadOnly]
		public LevelInfo levelInfo;

		public NativeHashMap<int, float2>.Concurrent patternMatchRequest;

		public EntityCommandBuffer.Concurrent cmdBuff;

		// -- //

		public void Execute(Entity entity, int entityIndex, ref SwapQuery query)
		{
			// Check if the blocks are adjacents
			var dist     = math.abs(query.gridPosB - query.gridPosA);
			var adjacent = math.max(dist.x, dist.y) <= 1 && math.max(dist.x, dist.y) > 0;

			if(adjacent)
			{
				var blockAIndex = MathHelpers.To1D(query.gridPosA, levelInfo.size.x);
				var blockAInfo  = level[blockAIndex];
				var blockBIndex = MathHelpers.To1D(query.gridPosB, levelInfo.size.x);
				var blockBInfo  = level[blockBIndex];

				level[blockAIndex] = blockBInfo;
				level[blockBIndex] = blockAInfo;

				patternMatchRequest.TryAdd(entityIndex, query.gridPosB);

				Debug.Log("New pattern matching query");
			}

			cmdBuff.DestroyEntity(entityIndex, entity);
		}
	}

	[RequireComponentTag(typeof(Prefab))]
	struct PatternMatching : IJobForEachWithEntity<PatternInfo>
	{
		[ReadOnly]
		public BufferFromEntity<Pattern> patternsBufferLookup;

		[ReadOnly]
		public NativeHashMap<int, float2> patternMatchRequest;

		[ReadOnly]
		public BufferFromEntity<Level> levelBufferLookup;

		[ReadOnly]
		public LevelInfo levelInfo;

		// -- //

		public void Execute(Entity entity, int entityIndex, ref PatternInfo patternInfo)
		{
			if(patternMatchRequest.Length == 0)
				return;

			var requests = patternMatchRequest.GetValueArray(Allocator.Temp);
			var pattern  = patternsBufferLookup[entity];

			// Debug.Log("Query : " + requests.Length);

			for(int i = 0; i < requests.Length; ++i)
			{
				Match(pattern, ref patternInfo, requests[i]);
				// Debug.Log("Request !!!!");
			}

			requests.Dispose();
		}

		// -- //

		private void Match(DynamicBuffer<Pattern> pattern, ref PatternInfo patternInfo, float2 gridPos)
		{
			var level        = levelBufferLookup[levelInfo.entity];
			var blockToMatch = level[MathHelpers.To1D(gridPos, levelInfo.size.x)];
			var height = patternInfo.size.y;
			var width  = patternInfo.size.x;
			var matchAll = true;

			for(int patternY = 0; patternY < height; ++patternY)
			{
				for(int patternX = 0; patternX < width; ++patternX)
				{
					// First : loop on the pattern to get the position in the level
					//			from which to start the pattern match

					matchAll = true;

					var patternOffset = new int2(patternX, patternY);
					var levelStart    = gridPos - patternOffset;

					for(int levelY = 0; levelY < height; ++levelY)
					{
						for(int levelX = 0; levelX < width; ++levelX)
						{
							// Second : loop on the pattern and level at the same time to check if block match

							var localPos = new int2(levelX, levelY);
							var blockPos = levelStart + localPos;

							var levelBufferId = MathHelpers.To1D(blockPos, levelInfo.size.x);

							if(levelBufferId < 0 || levelBufferId >= level.Length)
							{
								matchAll = false;
								break;
							}

							var block    = level[levelBufferId];
							var shouldMatch = pattern[MathHelpers.To1D(localPos, width)].match;
							var matchThis   = !shouldMatch || block.blockId == blockToMatch.blockId;

							matchAll = matchAll && matchThis;

							Debug.Log($"Pos : {blockPos} | Local : {localPos} | Should match ? {shouldMatch} | Match {blockToMatch.blockId} with {block.blockId} | Match this ? {matchThis} | All ? {matchAll}");


							// TODO break early if no match
						}
					}
				}

				if(matchAll)
					break;
			}

			Debug.Log($"Match ? {matchAll}");
		}
	}
}

}