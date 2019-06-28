using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Game.GameElements.Runtime;
using Game.Hybrid;
using Game.Command;

namespace Game.View
{

public class HybridViewSystem : ComponentSystem
{
	// PRIVATES

	private EntityQuery deleteBlocksQuery;
	private ViewCmdBuffer cmdBuffer;
	private ViewCommandStack viewCmdStack;
	
	// LIFE-CYCLE
	
	protected override void OnCreateManager()
	{
		deleteBlocksQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<DeleteCommand>()
		);

		cmdBuffer    = World.GetOrCreateSystem<ViewCmdBuffer>();
		viewCmdStack = CommandStack.Get<ViewCommandStack>(100);
	}
	
	protected override void OnUpdate()
	{
		if(viewCmdStack.HasCommand<HighligthCommand>())
			return;
		
		Entities.With(deleteBlocksQuery).ForEach((Entity entity) => {
			EntityManager.DestroyHybrid(entity);
			PostUpdateCommands.DestroyEntity(entity);
		});
	}
}

}