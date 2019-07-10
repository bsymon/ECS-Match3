using System;
using Game.Command;

namespace Game.View
{

[Serializable]
public struct DeleteCommand : ICommand
{
	public int GetPriority()
	{
		return 20;
	}
}

}