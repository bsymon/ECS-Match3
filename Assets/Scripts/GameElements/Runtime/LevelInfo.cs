using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

[System.Serializable]
public struct LevelInfo : IComponentData
{

	public float2 levelSize;

}

}