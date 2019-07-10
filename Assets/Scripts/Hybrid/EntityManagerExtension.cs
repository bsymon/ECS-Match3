using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public static class EntityManagerExtension
{
	public static Entity Instantiate(this EntityManager entityManager, GameObjectEntity gameObject)
	{
		var instance = GameObject.Instantiate(gameObject);
		var entity   = GameObjectConversionUtility.ConvertGameObjectHierarchy(instance.gameObject, entityManager.World);
		instance.LinkedEntity = entity;

		entityManager.SetName(entity, instance.gameObject.name);
		entityManager.AddComponentObject(entity, instance.transform);
		entityManager.AddComponentObject(entity, instance);

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