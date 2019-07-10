using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using Game.GameElements.Runtime;
using Game.Command;

namespace Game.View
{

public class ViewCmdBuffer : EntityCommandBufferSystem { }

public class ViewSystem : JobComponentSystem
{

	// PRIVATES FIELDS

	private ViewCmdBuffer cmdBuffer;
	private ViewCommandStack viewCmdStack;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		cmdBuffer    = World.GetOrCreateSystem<ViewCmdBuffer>();
		viewCmdStack = CommandStack.Get<ViewCommandStack>(100);

	}

	override protected JobHandle OnUpdate(JobHandle jobs)
	{
		var cmdBufferConcurrent = cmdBuffer.CreateCommandBuffer().ToConcurrent();
		var deltaTime = Time.deltaTime;

		var swapBlocks = new SwapBlock() {
			cmdBuffer = cmdBufferConcurrent
		};

		jobs = swapBlocks.Schedule(this, jobs);

		if(viewCmdStack.CanExecute<MoveDownCommand>())
		{
			var moveDownBlock = new MoveDown() {
				cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent(),
				dt        = deltaTime
			};

			jobs = moveDownBlock.Schedule(this, jobs);
		}

		return jobs;
	}

	// JOBS

	struct SwapBlock : IJobForEachWithEntity<Block, SwapCommand, Translation>
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

	[RequireComponentTag(typeof(Block))]
	struct DeleteBlock : IJobForEachWithEntity<DeleteCommand>
	{
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, [ReadOnly] ref DeleteCommand command)
		{
			cmdBuffer.DestroyEntity(index, entity);
		}
	}

}

}