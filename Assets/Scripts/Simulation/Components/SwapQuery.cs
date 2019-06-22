using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation
{

[Serializable]
public struct SwapQuery : IComponentData
{
	public int2 gridPosA;
	public int2 gridPosB;
}

}