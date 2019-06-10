using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Game.Command
{

public abstract class CommandStack : EntityCommandBufferSystem
{
	public struct Concurrent
	{
		public NativeMultiHashMap<int, ComponentType>.Concurrent commands;
		public EntityCommandBuffer.Concurrent cmdBuffer;

		// -- //

		public void AddCommand<T>(int index, Entity entity) where T : struct, ICommand
		{
			var c = new T();
			var d = ComponentType.ReadOnly<T>();

			commands.Add(c.GetPriority(), d);
			cmdBuffer.AddComponent(index, entity, c);
		}
	}

	// PRIVATES FIELDS

	private NativeMultiHashMap<int, ComponentType> commands;
	private EntityCommandBuffer cmdBuffer;

	private List<EntityQuery> queries;
	private Dictionary<int, int> cachedCommandsPrio;
	private Dictionary<int, int> cachedCommandsIndex;

	// PRIVATES METHODS

	private void CreateCommandsQueries()
	{
		if(commands.Length > 0)
		{
			var priorities = commands.GetKeyArray(Allocator.Temp);
			var values     = commands.GetValueArray(Allocator.Temp);

			priorities.Sort();

			for(int i = 0; i < priorities.Length; ++i)
			{
				var priority = priorities[i];
				var value    = values[i];

				CacheCommandType(value, priority);
			}

			priorities.Dispose();
			values.Dispose();
		}

		commands.Clear();
	}

	private void CacheCommandType(ComponentType command, int priority)
	{
		var commandCode = command.GetManagedType().GetHashCode();

		if(cachedCommandsPrio.ContainsKey(commandCode))
			return;

		var query = EntityManager.CreateEntityQuery(command);

		queries.Add(query);
		cachedCommandsPrio.Add(commandCode, priority);
		cachedCommandsIndex.Add(commandCode, queries.Count - 1);
	}

	// LIFE-CYCLE

	protected override void OnCreateManager()
	{
		queries = new List<EntityQuery>();
		cachedCommandsPrio  = new Dictionary<int, int>();
		cachedCommandsIndex = new Dictionary<int, int>();
	}

	protected override void OnDestroyManager()
	{
		commands.Dispose();
	}

	protected sealed override void OnUpdate()
	{
		base.OnUpdate();
		CreateCommandsQueries();

		// NOTE (Benjamin) the command buffer is deallocated after OnUpdate
		//					but IsCreated stay True, so I need to create a empty buffer
		cmdBuffer = default(EntityCommandBuffer);
	}

	// INTERFACES

	public void AddCommand<T>(Entity entity) where T : struct, ICommand
	{
		if(!cmdBuffer.IsCreated)
			cmdBuffer = CreateCommandBuffer();

		var c = new T();
		var d = ComponentType.ReadOnly<T>();

		commands.Add(c.GetPriority(), d);
		cmdBuffer.AddComponent(entity, c);
	}

	public Concurrent ToConcurrent()
	{
		if(!cmdBuffer.IsCreated)
			cmdBuffer = CreateCommandBuffer();

		return new Concurrent() {
			commands  = commands.ToConcurrent(),
			cmdBuffer = cmdBuffer.ToConcurrent()
		};
	}

	// TODO (Benjamin) method to check if there is highest priority commands than a given one

	// STATIC METHODS

	public static T Create<T>(int stackCapacity) where T : CommandStack
	{
		var commandStack = World.Active.GetOrCreateSystem<T>();

		if(!commandStack.commands.IsCreated)
			commandStack.commands = new NativeMultiHashMap<int, ComponentType>(stackCapacity, Allocator.Persistent);

		return commandStack;
	}
}

}