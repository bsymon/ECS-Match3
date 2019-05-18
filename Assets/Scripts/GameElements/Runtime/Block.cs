using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

public struct Block : IComponentData
{

	public int blockId;
	public float2 gridPosition;

}

}