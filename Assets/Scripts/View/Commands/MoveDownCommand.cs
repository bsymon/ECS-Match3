using Unity.Mathematics;
using Game.Command;

namespace Game.View
{

[System.Serializable]
public struct MoveDownCommand : ICommand
{
	public int GetPriority()
	{
		return 30;
	}

	// PUBLICS FIELDS

	public int2 destination;
	public float duration;
	public float remain;

	// PRIVATES FIELDS

	private float3 startPos;

	// ACCESSORS

	public float3 StartPos
	{
		get { return startPos; }
		set
		{
			startPos    = value;
			StartPosSet = true;
		}
	}

	public bool StartPosSet
	{
		get; private set;
	}

	// INTERFACES

	public MoveDownCommand(int2 destination, float duration)
	{
		this.destination = destination;
		this.duration    = duration;
		this.remain      = duration;
		this.startPos    = float3.zero;
		this.StartPosSet = false;
	}
}

}