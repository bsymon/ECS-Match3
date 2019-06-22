using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

public struct Block : IComponentData
{

	public int blockId;
	public int2 gridPosition;

}

}