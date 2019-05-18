using System.Collections;
using System.Collections.Generic;
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
	private int patternWidth = 0;

	[SerializeField]
	private int patternHeight = 0;

	[SerializeField]
	private bool[] pattern = null;

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		var patternBuffer = dstManager.AddBuffer<Runtime.Pattern>(entity);

		for(var i = 0; i < patternHeight * patternWidth; ++i)
		{
			patternBuffer.Add(pattern[i]);
		}

		dstManager.AddComponentData(entity, new Runtime.PatternInfo() {
			patternSize  = new float2(patternWidth, patternHeight),
		});
	}
}

}