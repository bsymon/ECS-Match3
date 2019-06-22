using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.GameElements.Runtime;
using Game.Command;
using Game.View;

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
	private NativeHashMap<int, int2> patternMatchRequest;
	private NativeHashMap<int, bool> unswap;

	private EntityQuery patternsQuery;
	private NativeArray<PatternInfo> patterns;

	private ViewCommandStack viewCmdStack;

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		cmdBuffer    = World.GetOrCreateSystem<SimulationCmdBuffer>();
		viewCmdStack = CommandStack.Get<ViewCommandStack>(100);

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
		patterns.Sort(new PatternSortByPriority());

		patternMatchRequest = new NativeHashMap<int, int2>(10, Allocator.Persistent);
		unswap              = new NativeHashMap<int, bool>(10, Allocator.Persistent);
		GetLevelInfo();
	}

	protected override void OnStopRunning()
	{
		patterns.Dispose();
		patternMatchRequest.Dispose();
		unswap.Dispose();
	}

	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		if(patternMatchRequest.IsCreated)
		{
			patternMatchRequest.Clear();
			unswap.Clear();
		}

		var blocksToDelete = new NativeArray<int>(20, Allocator.TempJob);
		var jobCmdBuffer   = cmdBuffer.CreateCommandBuffer().ToConcurrent();

		var swapTestJob = new SwapTest() {
			level     = EntityManager.GetBuffer<Level>(levelEntity),
			levelInfo = level,
			cmdBuff   = jobCmdBuffer,
			patternMatchRequest = patternMatchRequest.ToConcurrent(),
			viewCmdStack = viewCmdStack.ToConcurrent()
		};

		var patternMatching = new PatternMatching() {
			patternsBufferLookup = GetBufferFromEntity<Pattern>(isReadOnly: true),
			patternMatchRequest  = patternMatchRequest,
			patternsInfo         = patterns,
			levelBufferLookup    = GetBufferFromEntity<Level>(isReadOnly: true),
			levelInfo            = level,
			blocksToDelete       = blocksToDelete,
			unswap = unswap.ToConcurrent()
		};

		var performSwap = new PerformSwap() {
			level     = EntityManager.GetBuffer<Level>(levelEntity),
			levelInfo = level,
			unswap    = unswap,
			cmdBuffer = jobCmdBuffer,
			viewCmdStack = viewCmdStack.ToConcurrent()
		};

		var deleteBlocks = new DeleteBlock() {
			blocksToDelete    = blocksToDelete,
			levelBufferLookup = GetBufferFromEntity<Level>(isReadOnly: false),
			levelInfo         = level,
			cmdBuffer         = jobCmdBuffer,
			viewCmdStack = viewCmdStack.ToConcurrent()
		};

		var moveDownBlocks = new MoveDownBlocks() {
			levelInfo         = level,
			levelBufferLookup = GetBufferFromEntity<Level>(isReadOnly: false),
			viewCmdStack      = viewCmdStack.ToConcurrent()
		};

		jobs = swapTestJob.Schedule(this, jobs);
		jobs = patternMatching.Schedule(jobs);
		jobs = performSwap.Schedule(this, jobs);
		jobs = deleteBlocks.Schedule(blocksToDelete.Length, 2, jobs);
		jobs = moveDownBlocks.Schedule(level.size.x, 2, jobs);

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

		public NativeHashMap<int, int2>.Concurrent patternMatchRequest;

		public EntityCommandBuffer.Concurrent cmdBuff;

		public ViewCommandStack.Concurrent viewCmdStack;

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
		}
	}

	struct PatternMatching : IJob
	{
		[ReadOnly]
		public NativeHashMap<int, int2> patternMatchRequest;

		[ReadOnly]
		public BufferFromEntity<Pattern> patternsBufferLookup;

		[ReadOnly]
		public NativeArray<PatternInfo> patternsInfo;

		[ReadOnly]
		public BufferFromEntity<Level> levelBufferLookup;

		[ReadOnly]
		public LevelInfo levelInfo;

		public NativeArray<int> blocksToDelete;

		public NativeHashMap<int, bool>.Concurrent unswap;

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
				var request = requests[i];
				var match   = false;

				for(int j = 0; j < patternsInfo.Length; ++j)
				{
					var patternInfo   = patternsInfo[j];
					var pattern       = patternsBufferLookup[patternInfo.entity];
					var matchedBlocks = new NativeArray<int>(pattern.Length, Allocator.Temp);

					match = Match(pattern, ref patternInfo, request, matchedBlocks);

					if(match)
						AddMatchedBlockToDelete(matchedBlocks);

					matchedBlocks.Dispose();

					if(match)
						break;
				}

				if(!match)
				{
					unswap.TryAdd(request.GetHashCode(), true);
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
				int2 gridPos, NativeArray<int> matchedBlocks)
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

							var block       = level[levelBufferId];
							var shouldMatch = pattern[MathHelpers.To1D(localPos, width)].match;
							var matchThis   = !shouldMatch || block.blockId == blockToMatch.blockId;

							matchAll = matchAll && matchThis;

							if(matchThis && shouldMatch)
							{
								matchedBlocks[blockMatched] = levelBufferId + 1;
								blockMatched++;
							}

							Debug.Log($"Pos : {blockPos} | Local : {localPos} | Should match ? {shouldMatch} | Match {blockToMatch.blockId} with {block.blockId} | Match this ? {matchThis} | All ? {matchAll}");

							// TODO break early if no match
						}
					}

					if(matchAll)
						break;
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

		public ViewCommandStack.Concurrent viewCmdStack;

		// -- //

		public void Execute(int index)
		{
			var blockId = blocksToDelete[index] - 1;

			if(blockId < 0)
				return;

			var level = levelBufferLookup[levelInfo.entity];
			var block = level[blockId];

			if(block.IsEmpty)
				return;

			level[blockId] = Level.Empty;

			viewCmdStack.AddCommand(index, block.entity, new HighligthCommand(duration: 0.5f));
			viewCmdStack.AddCommand<DeleteCommand>(index, block.entity);
		}
	}

	struct PerformSwap : IJobForEachWithEntity<SwapQuery>
	{
		[ReadOnly]
		public DynamicBuffer<Level> level;

		[ReadOnly]
		public LevelInfo levelInfo;

		[ReadOnly]
		public NativeHashMap<int, bool> unswap;

		public ViewCommandStack.Concurrent viewCmdStack;
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref SwapQuery query)
		{
			var blockAIndex = MathHelpers.To1D(query.gridPosA, levelInfo.size.x);
			var blockAInfo  = level[blockAIndex];
			var blockBIndex = MathHelpers.To1D(query.gridPosB, levelInfo.size.x);
			var blockBInfo  = level[blockBIndex];

			if(unswap.TryGetValue(query.gridPosB.GetHashCode(), out var unswapRequest))
			{
				level[blockAIndex] = blockBInfo;
				level[blockBIndex] = blockAInfo;
			}
			else
			{
				viewCmdStack.AddCommand(index, blockAInfo.entity, new SwapCommand() { destination = query.gridPosA });
				viewCmdStack.AddCommand(index, blockBInfo.entity, new SwapCommand() { destination = query.gridPosB });
			}

			cmdBuffer.DestroyEntity(index, entity);
		}
	}

	/** Loop the level on X, and each Execute loop on Y */
	struct MoveDownBlocks : IJobParallelFor
	{
		// NOTE (Benjamin) this job is always executed
		//					should only be triggered when the level actually change

		[ReadOnly]
		public LevelInfo levelInfo;

		[ReadOnly]
		public BufferFromEntity<Level> levelBufferLookup;

		public ViewCommandStack.Concurrent viewCmdStack;

		// -- //

		public void Execute(int index)
		{
			var level = levelBufferLookup[levelInfo.entity];

			if(level.Length == 0)
				return;

			var x       = index;
			var lowestY = -1;

			for(int y = 0; y < levelInfo.size.y; ++y)
			{
				var pos2D     = new int2(x, y);
				var pos1D     = MathHelpers.To1D(pos2D, levelInfo.size.x);
				var blockInfo = level[pos1D];

				if(blockInfo.IsEmpty && lowestY == -1)
				{
					lowestY = pos1D;
				}

				if(!blockInfo.IsEmpty && lowestY > -1)
				{
					var blockDest  = MathHelpers.To2D(lowestY, levelInfo.size.x);
					level[lowestY] = blockInfo;
					level[pos1D]   = Level.Empty;
					lowestY        = lowestY + levelInfo.size.x;

					viewCmdStack.AddCommand(index, blockInfo.entity, new MoveDownCommand(destination: blockDest, duration: 2f));
				}
			}
		}
	}
}

}