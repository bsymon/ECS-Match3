using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public class LinkGameObjectAndEntitySystem : ComponentSystem
{
	// PRIVATES

	private EntityQuery query;
	
	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		query = GetEntityQuery(
			ComponentType.ReadWrite<Runtime.LinkGameObjectAndEntity>()
		);
	}
	
	protected override void OnUpdate()
	{
		Entities.With(query).ForEach((Entity entity, ref Runtime.LinkGameObjectAndEntity link) => {
			var goLink = EntityManager.GetComponentObject<LinkGameObjectAndEntity>(entity);

			if(goLink != null)
			{
				// Debug.Log($"Time : {goLink.yolo}");
				goLink.yolo = Time.realtimeSinceStartup;
			}
		});
	}
}

}