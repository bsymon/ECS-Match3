using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public class ForwardAnimationEvents : MonoBehaviour, IConvertGameObjectToEntity
{
	// INSPECTOR

	[SerializeField]
	private GameObjectEntity gameObjectEntity = null;

	// EVENTS

	public event System.Action<string, Entity> OnEvent;

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new Runtime.ForwardAnimationEvents());
	}

	// PRIVATES METHODS

	private void ForwardEvent(string eventName)
	{
		if(OnEvent != null)
			OnEvent(eventName, gameObjectEntity.LinkedEntity);
	}
}

}