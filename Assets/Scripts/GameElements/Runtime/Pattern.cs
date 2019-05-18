using UnityEngine;
using Unity.Entities;

namespace Game.GameElements.Runtime
{

[System.Serializable]
[InternalBufferCapacity(100)]
public struct Pattern : IBufferElementData
{

	public static implicit operator bool(Pattern e) { return e.match; }
	public static implicit operator Pattern(bool e) { return new Pattern(){ match = e }; }

	/* True: match block, False: match any block */
	public bool match;

}

}