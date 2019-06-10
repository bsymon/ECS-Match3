using Unity.Entities;

namespace Game.Command
{

public interface ICommand : IComponentData
{
	int GetPriority();
}

}