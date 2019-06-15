using Game.Command;

namespace Game.View
{

public struct HighligthCommand : ICommand
{
	public int GetPriority()
	{
		return 0;
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