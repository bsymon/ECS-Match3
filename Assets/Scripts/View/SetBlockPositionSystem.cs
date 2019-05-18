using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Game.GameElements.Runtime;

namespace Game.View
{

public class SetBlockPositionSystem : ComponentSystem
{

	// PRIVATES FIELDS

	private EntityQuery blocksQuery;

	// LIFE-CYCLE

	override protected void OnCreateManager()
	{
		blocksQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadWrite<Translation>()
		);
	}

	override protected void OnUpdate()
	{
		// TODO (Benjamin) will be execute each frame
		//					should add a flag component to only do it when needed

		var i = 0;

		Entities.With(blocksQuery).ForEach((Entity ent, ref Translation t, ref Block b) => {
			var worldPos = new float3(b.gridPosition * 2f, 0);
			t.Value = worldPos;

			i++;
		});
	}

}

}