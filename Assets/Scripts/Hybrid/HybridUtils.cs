using Unity.Entities;

namespace Game.Hybrid
{

public struct ConvertedGameObject
{
	public Entity entity;
	public GameObjectEntity gameObject;
}

public static class HybridUtils
{
	public static ConvertedGameObject ConvertGameObjectHierarchy(GameObjectEntity gameObject, World dstEntityWorld)
	{
		var entityManager = dstEntityWorld.EntityManager;
		
		gameObject.ObjectRenderer.enabled = false;
		
		// foreach(var component in gameObject.IgnoredComponents)
		// 	component.enabled = false;

		var prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject.gameObject, dstEntityWorld);

		entityManager.SetName(prefabEntity, gameObject.gameObject.name);
		entityManager.AddComponentObject(prefabEntity, gameObject);

		gameObject.LinkedEntity = prefabEntity;
		gameObject.ObjectRenderer.enabled = true;

		// foreach(var component in gameObject.IgnoredComponents)
		// 	component.enabled = true;
		
		return new ConvertedGameObject() {
			entity     = prefabEntity,
			gameObject = gameObject
		};
	}
}

}