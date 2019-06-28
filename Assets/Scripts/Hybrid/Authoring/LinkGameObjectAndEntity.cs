using Unity.Entities;
using UnityEngine;

namespace Game.Hybrid
{

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class LinkGameObjectAndEntity : MonoBehaviour, IConvertGameObjectToEntity
{
	public float yolo;
	
	
	
	// PRIVATES

	private Entity entity;
	
	// INTERFACES
	
	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		this.entity       = entity;
		var linkComponent = new Runtime.LinkGameObjectAndEntity() {gameObjectUUID = gameObject.GetInstanceID()};

		dstManager.AddComponentData(entity, linkComponent);
		dstManager.AddComponentObject(entity, this);
	}
}

}
