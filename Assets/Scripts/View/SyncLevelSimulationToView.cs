using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using Game.GameElements.Runtime;

namespace Game.View
{

class SyncLevelBarrier : EntityCommandBufferSystem { }

public class SyncLevelSimulationToView : JobComponentSystem
{
	// PRIVATES FIELDS

	private SyncLevelBarrier cmdBuffer;

	private EntityQuery levelQuery;
	private LevelInfo level;
	private Entity levelEntity;

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		cmdBuffer = World.GetOrCreateSystem<SyncLevelBarrier>();

		levelQuery = GetEntityQuery(
			ComponentType.ReadOnly<LevelInfo>()
		);
	}

	protected override void OnStartRunning()
	{
		GetLevelInfo();
	}

	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		var loopCount = (int) (level.size.x * level.size.y);
		var syncLevel = new SyncLevel() {
			entityToBuffer = GetBufferFromEntity<Level>(true),
			levelEntity    = levelEntity,
			levelInfo = level,
			cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent()
		};


		jobs = syncLevel.Schedule(loopCount, 2, jobs);

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

	struct SyncLevel : IJobParallelFor
	{
		[ReadOnly]
		public BufferFromEntity<Level> entityToBuffer;

		[ReadOnly]
		public Entity levelEntity;

		[ReadOnly]
		public LevelInfo levelInfo;

		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(int i)
		{
			var level = entityToBuffer[levelEntity];

			var blockInfo = level[i];

			if(blockInfo.IsEmpty)
				return;

			var block     = new Block() {
				blockId      = blockInfo.blockId,
				gridPosition = MathHelpers.To2D(i, (int) levelInfo.size.x)
			};

			cmdBuffer.SetComponent(i, blockInfo.entity, block);
		}
	}
}

}