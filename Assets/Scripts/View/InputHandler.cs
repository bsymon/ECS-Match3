using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Game.GameElements;
using Game.Simulation;

namespace Game.View
{

public class InputHandler : MonoBehaviour
{

	// INSPECTOR

	[SerializeField]
	private Level level = null;

	[SerializeField]
	private InputPlaceholder inputPlaceholderPrefab = null;

	[Header("Dependencies")]

	[SerializeField]
	private SimulationBridge simulation = null;

	// PRIVATES FIELDS

	private EntityManager entityManager;
	private PhysicsScene2D physicsScene;
	private new Camera camera;

	private int inputCastMask;
	private InputPlaceholder selectionA;
	private InputPlaceholder selectionB;

	// LIFE-CYCLE

	private void Awake()
	{
		inputCastMask = LayerMask.GetMask("Input");
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
			var hit         = physicsScene.GetRayIntersection(ray, 1000, inputCastMask);

			Debug.DrawRay(ray.origin, ray.direction * 15, Color.red, 3);

			if(hit.collider != null)
			{
				StoreSelection(hit.collider.gameObject);
			}
		}
	}

	// PRIVATES METHODS

	private void StoreSelection(GameObject mayebSelection)
	{
		var placeholder = mayebSelection.GetComponent<InputPlaceholder>();

		if(placeholder == null)
			return;

		if(selectionA == null)
			selectionA = placeholder;
		else if(selectionB == null)
			selectionB = placeholder;

		if(selectionA != null && selectionB != null)
		{
			simulation.CreateSwapQuery(selectionA.GridPosition, selectionB.GridPosition);

			selectionA = null;
			selectionB = null;
		}
	}

	private void CreatePlaceholder()
	{
		for(var y = 0; y < level.LevelSize.y; ++y)
		{
			for(var x = 0; x < level.LevelSize.x; ++x)
			{
				var newPlaceholder = Instantiate(inputPlaceholderPrefab);
				var worldPos       = new float3(x, y, 0) * 2f; // TODO (Benjamin) param for gridSize

				newPlaceholder.GridPosition       = new int2(x, y);
				newPlaceholder.transform.position = worldPos;
				newPlaceholder.transform.parent   = transform;
			}
		}
	}

}

}