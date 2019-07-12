using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Game.GameElements.Runtime;
using Game.Hybrid;
using Game.View;
using Game.Command;

namespace Game.Simulation
{

public class SpawnBlockInLevelSystem : ComponentSystem
{

	// PRIVATES FIELDS

	private SimulationSystem simulationSystem;
	private ViewCommandStack viewCmdStack;

	private EntityQuery blockPrefabsQuery;
	private bool blockPrefabsInit;
	private Entity[] blockPrefabs;

	private EntityQuery levelQuery;
	private Entity levelEntity;
	private LevelInfo levelInfo;

	private bool levelFirstInit;
	private bool wasMovingDownCommand;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		simulationSystem = World.GetOrCreateSystem<SimulationSystem>();
		viewCmdStack     = CommandStack.Get<ViewCommandStack>(100);

		blockPrefabsQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<Prefab>()
		);

		levelQuery = GetEntityQuery(
			ComponentType.ReadOnly<LevelInfo>()
		);

		wasMovingDownCommand = true;
	}

	protected override void OnStartRunning()
	{
		levelInfo = GetLevelInfo(out levelEntity);
		InitBlockPrefabs();
	}

	override protected void OnUpdate()
	{
		if(!levelFirstInit)
		{
			InitLevel(levelEntity, levelInfo);
		}

		var currentMovingDownCommand = viewCmdStack.HasCommand<MoveDownCommand>();

		if(!simulationSystem.HasPendingMatchRequests && !currentMovingDownCommand && wasMovingDownCommand)
			SpawnBlock(levelEntity, levelInfo);

		wasMovingDownCommand = currentMovingDownCommand;
	}

	// PRIVATES METHODS

	private void InitBlockPrefabs()
	{
		var nativeArray  = blockPrefabsQuery.ToEntityArray(Allocator.TempJob);
		blockPrefabs     = nativeArray.ToArray();
		blockPrefabsInit = true;

		nativeArray.Dispose();
	}

	private LevelInfo GetLevelInfo(out Entity levelEntity)
	{
		var compSet = false;
		LevelInfo temp_Level    = default(LevelInfo);
		Entity temp_LevelEntity = Entity.Null;

		Entities.With(levelQuery).ForEach((Entity entity, ref LevelInfo levelInfo) => {
			if(!compSet)
			{
				temp_Level       = levelInfo;
				temp_LevelEntity = entity;
				compSet          = true;
			}
		});

		levelEntity = temp_LevelEntity;

		return temp_Level;
	}

	private void InitLevel(Entity levelEntity, LevelInfo levelInfo)
	{
		var levelBuffer = EntityManager.GetBuffer<Level>(levelEntity);

		for(var y = 0; y < levelInfo.size.y; ++y)
		{
			for(var x = 0; x < levelInfo.size.x; ++x)
			{
				levelBuffer.Add(Level.Empty);
			}
		}

		levelFirstInit = true;
	}

	private void SpawnBlock(Entity levelEntity, LevelInfo levelInfo)
	{
		for(var y = 0; y < levelInfo.size.y; ++y)
		{
			for(var x = 0; x < levelInfo.size.x; ++x)
			{
				var i = x + levelInfo.size.x * y;
				var levelBuffer = EntityManager.GetBuffer<Level>(levelEntity);
				var currentBlock = levelBuffer[i];

				if(!currentBlock.IsEmpty)
					continue;

				var blockPrefab = blockPrefabs[UnityEngine.Random.Range(0, blockPrefabs.Length)];
				var blockEntity = EntityManager.InstantiateHybrid(blockPrefab);
				var block       = EntityManager.GetComponentData<Block>(blockEntity);
				var translation = EntityManager.GetComponentData<Translation>(blockEntity);

				block.gridPosition   = new int2(x, y);
				currentBlock.blockId = block.blockId;
				currentBlock.entity  = blockEntity;
				translation.Value    = new float3(block.gridPosition * 2, 0);

				levelBuffer = EntityManager.GetBuffer<Level>(levelEntity); // NOTE (Benjamin) get the buffer again, because it is deallocated after any method call of EntityManager ...

				levelBuffer[i] = currentBlock;

				EntityManager.SetComponentData(blockEntity, block);
				EntityManager.SetComponentData(blockEntity, translation);

				var transform = EntityManager.GetComponentObject<UnityEngine.Transform>(blockEntity);
				transform.position = translation.Value;
				transform.localScale = new float3(1,1,1);
			}
		}
	}

}

}