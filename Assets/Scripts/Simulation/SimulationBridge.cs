using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation
{

public class SimulationBridge : MonoBehaviour
{
	// PRIVATES FIELDS

	private EntityManager entityManager;

	// LIFE-CYCLE

	private void Awake()
	{
		entityManager = World.Active.EntityManager;
	}

	// INTERFACES

	public void CreateSwapQuery(int2 gridPosA, int2 gridPosB)
	{
		var entity = entityManager.CreateEntity();
		var query  = new SwapQuery() {
			gridPosA = gridPosA,
			gridPosB = gridPosB
		};

		entityManager.AddComponentData(entity, query);
		entityManager.SetName(entity, "SwapQuery");
	}
}

}