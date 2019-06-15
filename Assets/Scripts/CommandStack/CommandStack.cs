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
			AddCommand(index, entity, new T());
		}

		public void AddCommand<T>(int index, Entity entity, T command) where T : struct, ICommand
		{
			var d = ComponentType.ReadOnly<T>();

			commands.Add(command.GetPriority(), d);
			cmdBuffer.AddComponent(index, entity, command);
		}
	}

	// PRIVATES FIELDS

	private NativeMultiHashMap<int, ComponentType> commands;
	private EntityCommandBuffer cmdBuffer;

	private List<EntityQuery> queries;
	private Dictionary<int, int> cachedCommandsPrio;	// Key : CommandType hash - Value : priority
	private Dictionary<int, int> cachedCommandsIndex;	// Key : CommandType hash - Value : index in "queries"

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
		AddCommand<T>(entity, new T());
	}

	public void AddCommand<T>(Entity entity, T command) where T : struct, ICommand
	{
		if(!cmdBuffer.IsCreated)
			cmdBuffer = CreateCommandBuffer();

		var d = ComponentType.ReadOnly<T>();

		commands.Add(command.GetPriority(), d);
		cmdBuffer.AddComponent(entity, command);
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

	public bool HasCommand<T>() where T : struct, ICommand
	{
		var command = new T(); // NOTE (Benjamin) should find another way to get the priority
								//				instead of creating a new instance
		var commandPriority = command.GetPriority();
		var commandsRemain  = 0;

		foreach(var item in cachedCommandsPrio)
		{
			var prio  = item.Value;
			var index = cachedCommandsIndex[item.Key];

			if(prio > commandPriority)
				continue;

			var query = queries[index];
			commandsRemain += query.CalculateLength();
		}

		return commandsRemain > 0;
	}

	// STATIC METHODS

	public static T Get<T>(int stackCapacity) where T : CommandStack
	{
		var commandStack = World.Active.GetOrCreateSystem<T>();

		if(!commandStack.commands.IsCreated)
			commandStack.commands = new NativeMultiHashMap<int, ComponentType>(stackCapacity, Allocator.Persistent);

		return commandStack;
	}
}

}