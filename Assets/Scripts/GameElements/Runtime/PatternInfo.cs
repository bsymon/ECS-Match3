using UnityEngine;
using Unity.Entities;

namespace Game.GameElements.Runtime
{

[System.Serializable]
public struct PatternInfo : IComponentData
{

	public int width;
	public int height;

}

}