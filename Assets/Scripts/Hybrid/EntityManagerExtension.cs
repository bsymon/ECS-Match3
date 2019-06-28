﻿using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public static class EntityManagerExtension
{
	public static Entity Instantiate(this EntityManager entityManager, GameObjectEntity gameObject)
	{
		var instance = GameObject.Instantiate(gameObject);
		var entity   = entityManager.Instantiate(gameObject.LinkedEntity);
		instance.LinkedEntity = entity;

		entityManager.SetName(entity, instance.gameObject.name);

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

	public static void Destroy(this EntityManager entityManager, GameObjectEntity gameObject)
	{
		entityManager.DestroyEntity(gameObject.LinkedEntity);
		GameObject.Destroy(gameObject.gameObject);
	}
}

}