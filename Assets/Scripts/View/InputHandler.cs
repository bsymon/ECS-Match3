using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Game.GameElements;

namespace Game.View
{

public class InputHandler : MonoBehaviour
{

	// INSPECTOR

	[SerializeField]
	private Level level = null;

	[SerializeField]
	private BoxCollider2D inputPlaceholderPrefab = null;

	// PRIVATES FIELDS

	private EntityManager entityManager;
	private PhysicsScene2D physicsScene;
	private new Camera camera;

	// LIFE-CYCLE

	private void Awake()
	{
		entityManager = World.Active.EntityManager;
		physicsScene  = PhysicsSceneExtensions2D.GetPhysicsScene2D(SceneManager.GetActiveScene());
		camera        = Camera.main;

		CreatePlaceholder();
	}

	private void Update()
	{
		if(Input.GetMouseButtonUp(0))
		{
			var mousePos    = Input.mousePosition;
			var mouseCamPos = camera.ScreenToWorldPoint(mousePos);
			var ray         = new Ray(mouseCamPos, Vector3.forward);

			// TODO (Benjamin) cast the ray
			//					get the InputPlaceholder that was hit
			//					get its gridPosition
			//					highlight the placeholder
			//					store the selection
			//					if 2 placeholders have been selected, create a SwapQuery entity
		}
	}

	// PRIVATES METHODS

	private void CreatePlaceholder()
	{
		for(var y = 0; y < level.LevelSize.y; ++y)
		{
			for(var x = 0; x < level.LevelSize.x; ++x)
			{
				var newPlaceholder = Instantiate(inputPlaceholderPrefab);
				var worldPos       = new float3(x, y, 0) * 2f;

				newPlaceholder.transform.position = worldPos;
				newPlaceholder.transform.parent   = transform;
			}
		}
	}

}

}