using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Game.GameElements.Runtime;
using Game.Command;

namespace Game.View
{

class SyncLevelBarrier : EntityCommandBufferSystem { }

[UpdateAfter(typeof(ViewCommandStack))]
public class SyncLevelSimulationToView : JobComponentSystem
{
	// PRIVATES FIELDS

	private SyncLevelBarrier cmdBuffer;
	private ViewCommandStack viewCmdStack;

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		cmdBuffer    = World.GetOrCreateSystem<SyncLevelBarrier>();
		viewCmdStack = CommandStack.Get<ViewCommandStack>(100);
	}

	protected override JobHandle OnUpdate(JobHandle jobs)
	{
		var cmdBufferConcurrent = cmdBuffer.CreateCommandBuffer().ToConcurrent();
		var syncLevel = new SyncLevel() {
			cmdBuffer = cmdBufferConcurrent
		};

		jobs = syncLevel.Schedule(this, jobs);

		if(!viewCmdStack.HasCommand<HighligthCommand>())
		{
			var moveDownBlock = new MoveDown() {
				cmdBuffer = cmdBufferConcurrent,
				dt        = Time.deltaTime
			};

			jobs = moveDownBlock.Schedule(this, jobs);
		}

		if(!viewCmdStack.HasCommand<HighligthCommand>())
		{
			var deleteBlock = new DeleteBlock() {
				cmdBuffer = cmdBufferConcurrent
			};

			jobs = deleteBlock.Schedule(this, jobs);
		}

		return jobs;
	}

	// PRIVATES METHODS

	// JOBS

	struct SyncLevel : IJobForEachWithEntity<Block, SwapCommand, Translation>
	{
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref Block block,
				ref SwapCommand command, ref Translation translation)
		{
			var worldPos       = new float3(command.destination * 2, 0);
			block.gridPosition = command.destination;
			translation.Value  = worldPos;
			cmdBuffer.RemoveComponent<SwapCommand>(index, entity);
		}
	}

	struct MoveDown : IJobForEachWithEntity<Block, MoveDownCommand, Translation>
	{
		[ReadOnly]
		public float dt;

		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref Block block,
				ref MoveDownCommand command, ref Translation translation)
		{
			if(!command.StartPosSet)
				command.StartPos = translation.Value;

			var worldPos = new float3(command.destination * 2, 0);
			var progress = math.unlerp(command.duration, 0, command.remain);
			var position = math.lerp(command.StartPos, worldPos, progress);

			block.gridPosition = command.destination;
			translation.Value  = position;
			command.remain -= dt;

			if(progress >= 1)
			{
				translation.Value = worldPos;
				cmdBuffer.RemoveComponent<MoveDownCommand>(index, entity);
			}
		}
	}

	struct DeleteBlock : IJobForEachWithEntity<Block, DeleteCommand>
	{
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref Block block, [ReadOnly] ref DeleteCommand command)
		{
			cmdBuffer.DestroyEntity(index, entity);
		}
	}
}

}