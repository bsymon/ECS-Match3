using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements
{

public class Level : MonoBehaviour, IConvertGameObjectToEntity
{

	// INSPECTOR

	[SerializeField]
	private float2 levelSize;

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		var levelBuffer = dstManager.AddBuffer<Runtime.Level>(entity);
		var levelInfo   = new Runtime.LevelInfo() { levelSize = levelSize };

		dstManager.AddComponentData(entity, levelInfo);
		dstManager.SetName(entity, gameObject.name);
	}

}

}