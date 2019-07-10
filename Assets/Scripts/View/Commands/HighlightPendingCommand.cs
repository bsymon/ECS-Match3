using Game.Command;

namespace Game
{

public struct HighlightPendingCommand : ICommand
{
	public int GetPriority()
	{
		return 11;
	}
}

}