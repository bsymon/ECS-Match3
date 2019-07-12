using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

[System.Serializable]
public struct LevelInfo : IComponentData
{

	public int2 size;
	public float blockSize;
	public Entity entity;

}

}