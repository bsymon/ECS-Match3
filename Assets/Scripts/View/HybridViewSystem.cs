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
	// INJECT

	public Entity LevelEntiy { get; set; }
	public LevelInfo LevelInfo { get; set; }

	// PRIVATES

	private EntityQuery blocksWithAnimationQuery;
	private EntityQuery highlightBlocksQuery;
	private EntityQuery deleteBlocksQuery;
	private ViewCmdBuffer cmdBuffer;
	private ViewCommandStack viewCmdStack;

	private List<Entity> pendingDelete;

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		blocksWithAnimationQuery = GetEntityQuery(
			ComponentType.ReadOnly<Block>(),
			ComponentType.ReadOnly<Hybrid.Runtime.ForwardAnimationEvents>()
		);

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
		ListenAnimationEvents();

		if(viewCmdStack.CanExecute<HighligthCommand>())
			HighligthBlocks();

		if(viewCmdStack.CanExecute<DeleteCommand>())
			DeleteBlocks();
	}

	// PRIVATES METHODS

	private void ListenAnimationEvents()
	{
		Entities.With(blocksWithAnimationQuery).ForEach((Entity entity) => {
			var animationEvents      = EntityManager.GetComponentObject<ForwardAnimationEvents>(entity);
			animationEvents.OnEvent += HandleAnimationEvents;

			PostUpdateCommands.RemoveComponent<Hybrid.Runtime.ForwardAnimationEvents>(entity);
		});
	}

	private void HandleAnimationEvents(string eventName, Entity entity)
	{
		switch(eventName)
		{
			case "HighlighCommandOver":
				EntityManager.RemoveComponent<HighlightPendingCommand>(entity);
			break;
		}
	}

	private void HighligthBlocks()
	{
		Entities.With(highlightBlocksQuery).ForEach((Entity entity) => {
			var animator = EntityManager.GetComponentObject<Animator>(entity);

			animator.SetTrigger("Highlight");

			viewCmdStack.AddCommand<HighlightPendingCommand>(entity);
			PostUpdateCommands.RemoveComponent<HighligthCommand>(entity);
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