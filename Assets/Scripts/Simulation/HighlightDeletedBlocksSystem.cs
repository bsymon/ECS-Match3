using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

namespace Game.View
{

[UpdateAfter(typeof(ViewCommandStack))]
public class HighlightDeletedBlocksSystem : JobComponentSystem
{

	// PRIVATES FIELDS

	private EntityQuery query;
	private SyncLevelBarrier cmdBuffer;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		cmdBuffer = World.GetOrCreateSystem<SyncLevelBarrier>();
	}

	override protected JobHandle OnUpdate(JobHandle inputDeps)
	{
		var highlightJob = new Job() {
			dt        = Time.deltaTime,
			cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent()
		};

		inputDeps = highlightJob.Schedule(this, inputDeps);

		return inputDeps;
	}

	// JOBS

	struct Job : IJobForEachWithEntity<HighligthCommand, Scale>
	{
		public float dt;
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref HighligthCommand command, ref Scale scale)
		{
			var progress   = math.unlerp(command.duration, 0f, command.remain);
			var blockScale = math.lerp(1f, 2f, progress);

			scale.Value     = blockScale;
			command.remain -= dt;

			if(command.remain <= 0)
				cmdBuffer.RemoveComponent<HighligthCommand>(index, entity);
		}
	}

}

}