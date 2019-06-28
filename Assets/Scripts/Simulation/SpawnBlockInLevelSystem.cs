using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Game.GameElements.Runtime;
using Game.Hybrid;

namespace Game.Simulation
{

public class SpawnBlockInLevelSystem : ComponentSystem
{

	// PRIVATES FIELDS

	private EntityQuery blockPrefabsQuery;
	private bool blockPrefabsInit;
	private Entity[] blockPrefabs;

	private EntityQuery levelQuery;

	private bool levelFirstInit;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		blockPrefabsQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<Prefab>()
		);

		levelQuery = GetEntityQuery(
			ComponentType.ReadOnly<LevelInfo>()
		);
	}

	override protected void OnUpdate()
	{
		// TODO (Benjamin) maybe do init in OnStartRunning()
		if(!blockPrefabsInit)
			InitBlockPrefabs();

		var levelInfo = GetLevelInfo(out var levelEntity);

		if(!levelFirstInit)
		{
			InitLevel(levelEntity, levelInfo);
			SpawnBlock(levelEntity, levelInfo);
		}
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