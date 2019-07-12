using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements
{

public class Level : MonoBehaviour, IConvertGameObjectToEntity
{

	// INSPECTOR

	[SerializeField]
	private int2 levelSize;

	[SerializeField]
	private float blockSize;

	// ACCESSORS

	public int2 LevelSize => levelSize;

	public Entity LevelEntity
	{
		get; private set;
	}

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		LevelEntity = entity;

		var levelBuffer = dstManager.AddBuffer<Runtime.Level>(entity);
		var levelInfo   = new Runtime.LevelInfo() {
			size      = levelSize,
			entity    = entity,
			blockSize = blockSize
		};

		dstManager.AddComponentData(entity, levelInfo);
		dstManager.SetName(entity, gameObject.name);
		conversionSystem.SetSingleton(levelInfo);
	}

}

}