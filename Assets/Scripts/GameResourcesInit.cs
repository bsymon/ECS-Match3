﻿using UnityEngine;
using Unity.Entities;
using Game.GameElements;
using Game.Hybrid;

namespace Game
{

public class GameResourcesInit : MonoBehaviour
{

	// INSPECTOR

	[SerializeField]
	private Hybrid.GameObjectEntity[] blockPrefabs = null;

	[SerializeField]
	private Pattern[] patternPrefabs = null;

	[SerializeField]
	private BoxCollider2D inputPlaceholderPrefab = null;

	// PRIVATES FIELDS

	private EntityManager entityManager;

	// LIFE-CYCLE

	private void Awake()
	{
		entityManager = World.Active.EntityManager;

		LoadBlockPrefabs();
		LoadPatternPrefabs();
	}

	// PRIVATES METHODS

	private void LoadBlockPrefabs()
	{
		foreach(var blockPrefab in blockPrefabs)
		{
			var blockEntityPrefab = HybridUtils.ConvertGameObjectHierarchy(blockPrefab, World.Active);
		}
	}

	private void LoadPatternPrefabs()
	{
		foreach(var patternPrefab in patternPrefabs)
		{
			if(!patternPrefab.gameObject.activeSelf)
				continue;

			var blockEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(patternPrefab.gameObject, World.Active);

			entityManager.SetName(blockEntityPrefab, patternPrefab.gameObject.name);
		}
	}

}

}