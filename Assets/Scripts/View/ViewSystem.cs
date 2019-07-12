using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using Game.GameElements.Runtime;
using Game.GameElements;
using Game.Command;

namespace Game.View
{

public class ViewCmdBuffer : EntityCommandBufferSystem { }

public class ViewSystem : JobComponentSystem
{

	// PRIVATES FIELDS

	private ViewCmdBuffer cmdBuffer;
	private ViewCommandStack viewCmdStack;

	private EntityQuery levelQuery;
	private Entity levelEntity;
	private LevelInfo levelInfo;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		cmdBuffer    = World.GetOrCreateSystem<ViewCmdBuffer>();
		viewCmdStack = CommandStack.Get<ViewCommandStack>(100);
		levelQuery   = GetEntityQuery(ComponentType.ReadOnly<LevelInfo>());
	}

	protected override void OnStartRunning()
	{
		levelEntity = levelQuery.GetSingletonEntity();
		levelInfo   = levelQuery.GetSingleton<LevelInfo>();
	}

	override protected JobHandle OnUpdate(JobHandle jobs)
	{
		var cmdBufferConcurrent = cmdBuffer.CreateCommandBuffer().ToConcurrent();
		var deltaTime = Time.deltaTime;

		var swapBlocks = new SwapBlock() {
			cmdBuffer = cmdBufferConcurrent
		};

		var initBlockPos = new SetSpawnedBlockToInitialPosition() {
			levelInfo    = levelInfo,
			cmdBuffer    = cmdBufferConcurrent,
			viewCmdStack = viewCmdStack.ToConcurrent()
		};

		jobs = swapBlocks.Schedule(this, jobs);
		jobs = initBlockPos.Schedule(this, jobs);

		if(viewCmdStack.CanExecute<MoveDownCommand>())
		{
			var moveDownBlock = new MoveDown() {
				cmdBuffer = cmdBuffer.CreateCommandBuffer().ToConcurrent(),
				dt        = deltaTime,
				levelInfo = levelInfo
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

	[RequireComponentTag(typeof(SpawnedBlock))]
	struct SetSpawnedBlockToInitialPosition : IJobForEachWithEntity<Block, Translation>
	{
		public LevelInfo levelInfo;
		public EntityCommandBuffer.Concurrent cmdBuffer;
		public ViewCommandStack.Concurrent viewCmdStack;

		// -- //

		public void Execute(Entity entity, int index, ref Block block, ref Translation translation)
		{
			var worldPos = new float3((float2) block.gridPosition * levelInfo.blockSize, 0);
			worldPos.y  += levelInfo.size.y;

			translation.Value = worldPos;

			viewCmdStack.AddCommand(index, entity, new MoveDownCommand(destination: block.gridPosition, duration: 0.5f));
			cmdBuffer.RemoveComponent<SpawnedBlock>(index, entity);
		}
	}

	struct MoveDown : IJobForEachWithEntity<Block, MoveDownCommand, Translation>
	{
		public float dt;
		public LevelInfo levelInfo;

		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void Execute(Entity entity, int index, ref Block block,
				ref MoveDownCommand command, ref Translation translation)
		{
			if(!command.StartPosSet)
				command.StartPos = translation.Value;

			var worldPos = new float3((float2) command.destination * levelInfo.blockSize, 0);
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
}

}