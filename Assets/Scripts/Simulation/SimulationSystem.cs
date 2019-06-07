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
		GetLevelInfo();
	}
	
	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		var swapTestJob = new SwapTest() {
			level     = EntityManager.GetBuffer<Level>(levelEntity),
			levelInfo = level,
			cmdBuff   = cmdBuffer.CreateCommandBuffer().ToConcurrent()
		};

		jobs = swapTestJob.Schedule(this, jobs);
		
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

		public EntityCommandBuffer.Concurrent cmdBuff;
		
		// -- //
		
		public void Execute(Entity entity, int entityIndex, ref SwapQuery query)
		{
			// Check if the blocks are adjacents
			var dist     = math.abs(query.gridPosB - query.gridPosA);
			var adjacent = math.max(dist.x, dist.y) <= 1 && math.max(dist.x, dist.y) > 0;

			if(adjacent)
			{
				var blockAIndex = MathHelpers.To1D(query.gridPosA, levelInfo.levelSize.x);
				var blockAInfo  = level[blockAIndex];
				var blockBIndex = MathHelpers.To1D(query.gridPosB, levelInfo.levelSize.x);
				var blockBInfo  = level[blockBIndex];

				level[blockAIndex] = blockBInfo;
				level[blockBIndex] = blockAInfo;
			}

			cmdBuff.DestroyEntity(entityIndex, entity);
		}
	}
}

}