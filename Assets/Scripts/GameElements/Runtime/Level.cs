using UnityEngine;
using Unity.Entities;

namespace Game.GameElements.Runtime
{

[System.Serializable]
[InternalBufferCapacity(100)]
public struct Level : IBufferElementData
{

	public static implicit operator int(Level e) { return e.blockId; }
	public static implicit operator Level(int e) { return new Level(){ blockId = e }; }

	public int blockId;
	public Entity entity;

}

}