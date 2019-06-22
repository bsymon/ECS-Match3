using Unity.Mathematics;

namespace Game
{

public static class MathHelpers
{
	public static int To1D(int2 pos, int width)
	{
		return pos.x + width * pos.y;
	}

	public static int2 To2D(int pos, int with)
	{
		return new int2(pos % with, pos / with);
	}
}

}