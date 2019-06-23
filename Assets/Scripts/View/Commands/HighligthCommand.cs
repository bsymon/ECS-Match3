using Game.Command;

namespace Game.View
{

public struct HighligthCommand : ICommand
{
	public int GetPriority()
	{
		return 10;
	}

	// -- //

	public float duration;
	public float remain;

	// -- //

	public HighligthCommand(float duration)
	{
		this.duration = duration;
		this.remain   = duration;
	}
}

}