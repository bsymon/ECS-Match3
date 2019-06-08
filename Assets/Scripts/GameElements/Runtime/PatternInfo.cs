using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

[System.Serializable]
public struct PatternInfo : IComponentData
{

	public float2 size;

}

}