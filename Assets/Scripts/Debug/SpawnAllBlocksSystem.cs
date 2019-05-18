using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Game.GameElements.Runtime;

namespace Game.Debug
{

public class SpawnAllBlocksSystem : ComponentSystem
{

	// PRIVATES FIELDS

	private EntityQuery blockPrefabsQuery;

	private bool spawned;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		blockPrefabsQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<Prefab>()
		);
	}

	override protected void OnUpdate()
	{
		if(spawned)
			return;

		var i = 0;
		Entities.With(blockPrefabsQuery).ForEach((Entity entity) => {
			var block       = PostUpdateCommands.Instantiate(entity);
			var translation = new Translation() { Value = new float3(i * 2, 0, 0) };

			PostUpdateCommands.SetComponent(block, translation);

			i++;
		});

		spawned = true;
	}

}

}