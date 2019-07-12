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

	// ACCESSORS

	public int2 LevelSize
	{
		get { return levelSize; }
	}

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
			size   = levelSize,
			entity = entity
		};

		dstManager.AddComponentData(entity, levelInfo);
		dstManager.SetName(entity, gameObject.name);
		conversionSystem.SetSingleton(levelInfo);
	}

}

}