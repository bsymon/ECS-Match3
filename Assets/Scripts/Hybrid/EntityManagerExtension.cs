using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public static class EntityManagerExtension
{
	public static Entity Instantiate(this EntityManager entityManager, GameObjectEntity gameObject)
	{
		var instance = GameObject.Instantiate(gameObject);

		instance.ObjectRenderer.enabled = false;
		
		var entity   = GameObjectConversionUtility.ConvertGameObjectHierarchy(instance.gameObject, entityManager.World);
		instance.LinkedEntity = entity;

		foreach(var component in instance.ComponentsToLink)
			entityManager.AddComponentObject(entity, component);

		entityManager.SetName(entity, instance.gameObject.name);
		entityManager.AddComponentObject(entity, instance.transform);
		entityManager.AddComponentObject(entity, instance);

		instance.ObjectRenderer.enabled = true;

		return entity;
	}

	public static Entity InstantiateHybrid(this EntityManager entityManager, Entity srcEntity)
	{
		try
		{
			var gameObject = entityManager.GetComponentObject<GameObjectEntity>(srcEntity);
			return Instantiate(entityManager, gameObject);
		}
		catch(System.ArgumentException)
		{
			return entityManager.Instantiate(srcEntity);
		}
		catch(System.Exception)
		{
			throw;
		}
	}

	public static void DestroyHybrid(this EntityManager entityManager, Entity entity)
	{
		try
		{
			var gameObject = entityManager.GetComponentObject<GameObjectEntity>(entity);
			entityManager.DestroyEntity(gameObject.LinkedEntity);
			GameObject.Destroy(gameObject.gameObject);
		}
		catch(System.ArgumentException)
		{
			throw;
		}
	}
}

}