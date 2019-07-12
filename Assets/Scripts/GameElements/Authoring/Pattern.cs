using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements
{

[CreateAssetMenu(menuName="Game/Pattern")]
public class Pattern : ScriptableObject
{
	// INSPECTOR

	[SerializeField]
	private int2 size = int2.zero;

	[SerializeField]
	private bool[] pattern = null;

	// CONVERT

	public Entity Convert(EntityManager entityManager)
	{
		var entity        = entityManager.CreateEntity();
		var patternBuffer = entityManager.AddBuffer<Runtime.Pattern>(entity);
		var blocksCount   = 0;

		for(var i = 0; i < size.x * size.y; ++i)
		{
			blocksCount += pattern[i] ? 1 : 0;
			patternBuffer.Add(pattern[i]);
		}

		entityManager.AddComponentData(entity, new Runtime.PatternInfo() {
			size   = new int2(size.x, size.y),
			blocks = blocksCount,
			entity = entity
		});

		return entity;
	}
}

}