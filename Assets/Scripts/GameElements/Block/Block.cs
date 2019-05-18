﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Game.GameElements
{

public class Block : MonoBehaviour, IConvertGameObjectToEntity
{
	// INSPECTOR

	[SerializeField]
	private int blockId = 0;

	[SerializeField]
	private MeshRenderer sprite = null;

	[SerializeField]
	private Texture texture = null;

	// ACCESSORS

	public int BlockID
	{
		get { return blockId; }
	}

	// INTERFACES

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		var materialProp = new MaterialPropertyBlock();

		materialProp.SetTexture("_MainTex", texture);

		sprite.SetPropertyBlock(materialProp);

		dstManager.AddComponentData(entity, new Runtime.Block() { blockId = blockId });
	}
}

}