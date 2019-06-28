using UnityEngine;
using Unity.Entities;

namespace Game.Hybrid
{

public class GameObjectEntity : MonoBehaviour
{
	// INSPECTOR

	[SerializeField]
	private Renderer objectRenderer;

	[SerializeField]
	private Behaviour[] ignoredComponents;

	[SerializeField]
	private Component[] componentsToLink;

	// ACCESSORS

	public Renderer ObjectRenderer
	{
		get { return objectRenderer; }
	}

	public Behaviour[] IgnoredComponents
	{
		get { return ignoredComponents; }
	}

	public Component[] ComponentsToLink
	{
		get { return componentsToLink; }
	}

	public Entity LinkedEntity
	{
		get { return entity; }
		set
		{
			// if(entity == Entity.Null)
				entity = value;
		}
	}

	// PRIVATES

	private Entity entity;
}

}