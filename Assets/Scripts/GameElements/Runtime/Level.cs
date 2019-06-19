using UnityEngine;
using Unity.Entities;

namespace Game.GameElements.Runtime
{

[System.Serializable]
[InternalBufferCapacity(100)]
public struct Level : IBufferElementData
{

	// STATIC FIELDS

	public static Level Empty
	{
		get { return new Level() {blockId = -1, entity = Entity.Null}; }
	}

	public static implicit operator int(Level e) { return e.blockId; }
	public static implicit operator Level(int e) { return new Level(){ blockId = e }; }

	// ACCESSORS

	public bool IsEmpty
	{
		get { return blockId == -1; }
	}

	// PUBLICS FIELDS

	public int blockId;
	public Entity entity;

}

}