using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
		var syncLevel = new SyncLevel() {
			cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent()
		};

		jobs = syncLevel.Schedule(this, jobs);

		if(!viewCmdStack.HasCommand<HighligthCommand>())
		{
			var deleteBlock = new DeleteBlock() {
				cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent()
			};

			jobs = deleteBlock.Schedule(this, jobs);
		}

		return jobs;
	}

	// PRIVATES METHODS

	// JOBS

	struct SyncLevel : IJobForEachWithEntity<Block, SwapCommand>
	{
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref Block block, ref SwapCommand command)
		{
			block.gridPosition = command.destination;
			cmdBuffer.RemoveComponent<SwapCommand>(index, entity);
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