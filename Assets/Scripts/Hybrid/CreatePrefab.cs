using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

using Game.Hybrid;

public class CreatePrefab : MonoBehaviour
{
	// INSPECTORS

	[SerializeField]
	private Game.Hybrid.GameObjectEntity[] prefabs;

	// LIFE-CYCLE

	private void Start()
	{
		var entityManager = World.Active.EntityManager;

		foreach(var prefab in prefabs)
		{
			var converted = HybridUtils.ConvertGameObjectHierarchy(prefab, World.Active);

			entityManager.InstantiateHybrid(converted.entity);
		}
	}
}
