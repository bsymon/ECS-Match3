using Unity.Mathematics;
using Game.Command;

namespace Game.View
{

[System.Serializable]
public struct MoveDownCommand : ICommand
{
	public int GetPriority()
	{
		return 5;
	}

	// -- //

	public int2 destination;
	public float duration;
	public float remain;
	public float3 startPos;
	public bool startPosSet;

	// -- //

	public MoveDownCommand(int2 destination, float duration)
	{
		this.destination = destination;
		this.duration    = duration;
		this.remain      = duration;
		this.startPos    = float3.zero;
		this.startPosSet = false;
	}
}

}