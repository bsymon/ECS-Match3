using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Game.GameElements;

namespace Game
{

public class GameResourcesInit : MonoBehaviour
{

	// INSPECTOR

	[SerializeField]
	private Block[] blockPrefabs = null;

	// PRIVATES FIELDS

	private EntityManager entityManager;

	// LIFE-CYCLE

	private void Awake()
	{
		entityManager = World.Active.EntityManager;

		LoadBlockPrefabs();
	}

	// PRIVATES METHODS

	private void LoadBlockPrefabs()
	{
		foreach(var blockPrefab in blockPrefabs)
		{
			var blockEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(blockPrefab.gameObject, World.Active);

			entityManager.SetName(blockEntityPrefab, blockPrefab.gameObject.name);
		}
	}

}

}