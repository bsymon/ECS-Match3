using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.GameElements.Runtime
{

[System.Serializable]
public struct PatternInfo : IComponentData
{

	public int2 size;
	public int blocks;
	public Entity entity;

}

public struct PatternSortByPriority : IComparer<PatternInfo>
{
	public int Compare(PatternInfo a, PatternInfo b)
	{
		return b.blocks - a.blocks;
	}
}

}