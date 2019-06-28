using System;
using Unity.Entities;

namespace Game.Hybrid.Runtime
{

[Serializable]
public struct LinkGameObjectAndEntity : IComponentData
{
	public int gameObjectUUID;
}

}