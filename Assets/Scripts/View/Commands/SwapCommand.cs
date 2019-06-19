﻿using System;
using Unity.Mathematics;
using Game.Command;

namespace Game.View
{

[Serializable]
public struct SwapCommand : ICommand
{
	public int GetPriority()
	{
		return 10;
	}

	// -- //

	public float2 destination;
}

}