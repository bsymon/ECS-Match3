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

	private EntityQuery patternsQuery;
	private NativeArray<PatternInfo> patterns;

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

		patternsQuery = GetEntityQuery(
			ComponentType.ReadOnly<PatternInfo>(),
			ComponentType.ReadOnly<Prefab>()
		);
	}

	protected override void OnStartRunning()
	{
		patterns = patternsQuery.ToComponentDataArray<PatternInfo>(Allocator.Persistent);
		patternMatchRequest = new NativeHashMap<int, float2>(10, Allocator.Persistent);
		GetLevelInfo();
	}

	protected override void OnStopRunning()
	{
		patterns.Dispose();
		patternMatchRequest.Dispose();
	}

	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		if(patternMatchRequest.IsCreated)
		{
			patternMatchRequest.Clear();
		}

		var blocksToDelete = new NativeArray<int>(20, Allocator.TempJob);
		var jobCmdBuffer   = cmdBuffer.CreateCommandBuffer().ToConcurrent();

		var swapTestJob = new SwapTest() {
			level     = EntityManager.GetBuffer<Level>(levelEntity),
			levelInfo = level,
			cmdBuff   = jobCmdBuffer,
			patternMatchRequest = patternMatchRequest.ToConcurrent()
		};

		var patternMatching = new PatternMatching() {
			patternsBufferLookup = GetBufferFromEntity<Pattern>(isReadOnly: true),
			patternMatchRequest  = patternMatchRequest,
			patternsInfo         = patterns,
			levelBufferLookup    = GetBufferFromEntity<Level>(isReadOnly: true),
			levelInfo            = level,
			blocksToDelete        = blocksToDelete
		};

		var deleteBlocks = new DeleteBlock() {
			blocksToDelete    = blocksToDelete,
			levelBufferLookup = GetBufferFromEntity<Level>(isReadOnly: false),
			levelInfo         = level,
			cmdBuffer         = jobCmdBuffer
		};

		jobs = swapTestJob.Schedule(this, jobs);
		jobs = patternMatching.Schedule(jobs);
		jobs = deleteBlocks.Schedule(blocksToDelete.Length, 2, jobs);

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
	struct PatternMatching : IJob
	{
		[ReadOnly]
		public NativeHashMap<int, float2> patternMatchRequest;

		[ReadOnly]
		public BufferFromEntity<Pattern> patternsBufferLookup;

		[ReadOnly]
		public NativeArray<PatternInfo> patternsInfo;

		[ReadOnly]
		public BufferFromEntity<Level> levelBufferLookup;

		[ReadOnly]
		public LevelInfo levelInfo;

		public NativeArray<int> blocksToDelete;

		// -- //

		private int blockMatched;

		// -- //

		public void Execute()
		{
			if(patternMatchRequest.Length == 0)
				return;

			blockMatched = 0;
			var requests = patternMatchRequest.GetValueArray(Allocator.Temp);

			for(int i = 0; i < requests.Length; ++i)
			{
				for(int j = 0; j < patternsInfo.Length; ++j)
				{
					var patternInfo   = patternsInfo[j];
					var pattern       = patternsBufferLookup[patternInfo.entity];
					var matchedBlocks = new NativeArray<int>(pattern.Length, Allocator.Temp);
					var match         = Match(pattern, ref patternInfo, requests[i], matchedBlocks);

					if(match)
						AddMatchedBlockToDelete(matchedBlocks);

					matchedBlocks.Dispose();

					if(match)
						break;
				}
			}

			requests.Dispose();
		}

		// -- //

		private void AddMatchedBlockToDelete(NativeArray<int> blocks)
		{
			var addTo = blockMatched + blocks.Length;

			for(int i = blockMatched, j = 0; i < addTo; ++i, ++j)
			{
				blocksToDelete[i] = blocks[j];
			}
		}

		private bool Match(DynamicBuffer<Pattern> pattern, ref PatternInfo patternInfo,
				float2 gridPos, NativeArray<int> matchedBlocks)
		{
			var level        = levelBufferLookup[levelInfo.entity];
			var blockToMatch = level[MathHelpers.To1D(gridPos, levelInfo.size.x)];
			var height       = patternInfo.size.y;
			var width        = patternInfo.size.x;
			var matchAll     = true;
			var blockMatched = 0;

			for(int patternY = 0; patternY < height; ++patternY)
			{
				for(int patternX = 0; patternX < width; ++patternX)
				{
					// First : loop on the pattern to get the position in the level
					//			from which to start the pattern match

					matchAll     = true;
					blockMatched = 0;

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

							if(matchThis)
							{
								matchedBlocks[blockMatched] = levelBufferId + 1;
								blockMatched++;
							}

							Debug.Log($"Pos : {blockPos} | Local : {localPos} | Should match ? {shouldMatch} | Match {blockToMatch.blockId} with {block.blockId} | Match this ? {matchThis} | All ? {matchAll}");

							// TODO break early if no match
						}
					}
				}

				if(matchAll)
					break;
			}

			Debug.Log($"Match ? {matchAll}");

			return matchAll;
		}
	}

	struct DeleteBlock : IJobParallelFor
	{
		[ReadOnly]
		[DeallocateOnJobCompletion]
		public NativeArray<int> blocksToDelete;

		[ReadOnly]
		public BufferFromEntity<Level> levelBufferLookup;

		[ReadOnly]
		public LevelInfo levelInfo;

		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(int index)
		{
			var level   = levelBufferLookup[levelInfo.entity];
			var blockId = blocksToDelete[index] - 1;

			if(blockId < 0)
				return;

			var block = level[blockId];

			cmdBuffer.DestroyEntity(index, block.entity);
			level[blockId] = Level.Empty;
		}
	}
}

}