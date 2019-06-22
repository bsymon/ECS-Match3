using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements
{

public class Pattern : MonoBehaviour, IConvertGameObjectToEntity
{
	// TODO (Benjamin) don't use a GameObject to store pattern
	//					when converted, will pay the cost of all Transform convertion
	//					and systems overhead
	//					Use instead a ScriptableObject and maybe store it as BlobAssetReference ...

	// INSPECTOR

	[SerializeField]
	private int2 size = int2.zero;

	[SerializeField]
	private bool[] pattern = null;

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		var patternBuffer = dstManager.AddBuffer<Runtime.Pattern>(entity);
		var blocksCount   = 0;

		for(var i = 0; i < size.x * size.y; ++i)
		{
			blocksCount += pattern[i] ? 1 : 0;
			patternBuffer.Add(pattern[i]);
		}

		dstManager.AddComponentData(entity, new Runtime.PatternInfo() {
			size   = new int2(size.x, size.y),
			blocks = blocksCount,
			entity = entity
		});
	}
}

}