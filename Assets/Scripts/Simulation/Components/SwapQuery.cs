using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation
{

[Serializable]
public struct SwapQuery : IComponentData
{
	public float2 gridPosA;
	public float2 gridPosB;
}

}