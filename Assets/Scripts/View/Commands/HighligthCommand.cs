using Game.Command;

namespace Game.View
{

public struct HighligthCommand : ICommand
{
	public int GetPriority()
	{
		return 0;
	}
}

}