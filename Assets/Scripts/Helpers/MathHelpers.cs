using UnityEngine;
using Unity.Mathematics;

namespace Game
{

public static class MathHelpers
{
	public static int To1D(float2 pos, float width)
	{
		return (int) (pos.x + width * pos.y);
	}

	public static int2 To2D(int pos, int with)
	{
		return new int2(pos % with, pos / with);
	}
}

}