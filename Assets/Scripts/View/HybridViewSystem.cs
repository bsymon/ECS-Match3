using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Game.GameElements.Runtime;
using Game.Hybrid;
using Game.Command;

namespace Game.View
{

public class HybridViewSystem : ComponentSystem
{
	// PRIVATES

	private EntityQuery highlightBlocksQuery;
	private EntityQuery deleteBlocksQuery;
	private ViewCmdBuffer cmdBuffer;
	private ViewCommandStack viewCmdStack;

	private List<Entity> pendingDelete;
	
	// LIFE-CYCLE
	
	protected override void OnCreateManager()
	{
		highlightBlocksQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<HighligthCommand>()
		);
		
		deleteBlocksQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<DeleteCommand>()
		);

		cmdBuffer     = World.GetOrCreateSystem<ViewCmdBuffer>();
		viewCmdStack  = CommandStack.Get<ViewCommandStack>(100);
		pendingDelete = new List<Entity>();
	}
	
	protected override void OnUpdate()
	{
		if(!viewCmdStack.HasCommand<SwapCommand>())
			HighligthBlocks();
		
		if(!viewCmdStack.HasCommand<HighligthCommand>())
			DeleteBlocks();
	}

	// PRIVATES METHODS

	private void HighligthBlocks()
	{
		Entities.With(highlightBlocksQuery).ForEach((Entity entity) => {
			var animator = EntityManager.GetComponentObject<Animator>(entity);

			animator.SetTrigger("Highlight");
			Debug.Log("Set trigger");
		});
	}

	private void DeleteBlocks()
	{
		// TODO (Benjamin) use a custom EntityCommandBuffer to use as PostCommandBuffer, that allows to destroys GameObject
		
		Entities.With(deleteBlocksQuery).ForEach((Entity entity) => {
			pendingDelete.Add(entity);
		});

		foreach(var entity in pendingDelete)
			EntityManager.DestroyHybrid(entity);
		
		pendingDelete.Clear();
	}
}

}