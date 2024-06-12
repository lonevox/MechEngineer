using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using MechGrinder.Util;

namespace MechGrinder;

public partial class Cluster : RigidBody2D
{
	/// <summary>
	/// This is the number of mesh instances each MultiMesh will be created with.
	/// </summary>
	private const int InitialMultiMeshInstanceCapacity = 8;
	/// <summary>
	/// This is the number of floats of each instance within the cluster's MultiMeshes.
	/// See <see cref="RenderingServer.MultimeshSetBuffer"/> for an explanation of MultiMesh instance data.
	/// </summary>
	private const int MultiMeshInstanceFloatCount = 8;
	
	private static readonly float[] InitialMultiMeshBuffer = new float[InitialMultiMeshInstanceCapacity * MultiMeshInstanceFloatCount];
	/// <summary>
	/// Empty transform used for hiding mesh instances.
	/// </summary>
	private static readonly Transform2D ZeroTransform = new();

	public ControlMode ControlMode;
	// public readonly List<int> BlockIds = new List<int>();
	// public readonly List<Transform2D> BlockTransforms = new List<Transform2D>();
	private readonly List<Block> _blocks = new();
	/// <summary>
	/// The World that this Cluster exists in.
	/// </summary>
	[Export]
	public World? World;

	/// <summary>
	/// The RID of this body. Retrieved from <c>GetRid()</c> on construction.
	/// </summary>
	private readonly Rid _rid;
	/// <summary>
	/// Only <c>ConvexPolygonShape2D</c> shapes should be added to this shape owner.
	/// </summary>
	private readonly uint _shapeOwner;
	/// <summary>
	/// All unique block types of blocks that have been added to the cluster.
	/// </summary>
	private readonly HashSet<int> _usedBlockTypes = new();
	/// <summary>
	/// The cluster's multi meshes, keyed by BlockTypeID.
	/// </summary>
	private readonly Dictionary<int, ExpandableMultiMesh> _expandableMultiMeshes = new();
	/// <summary>
	/// Each _expandableMultiMeshes mesh instances, keyed by block ID.
	/// </summary>
	private readonly Dictionary<ExpandableMultiMesh, Dictionary<int, int>> _expandableMultiMeshInstances = new();

	private bool _freezeGraph;

	/// <summary>
	/// 
	/// </summary>
	private int _shapeOffset;

	public Cluster()
	{
		ContactMonitor = true;
		MaxContactsReported = 8;
		_rid = GetRid();
		_shapeOwner = CreateShapeOwner(this);
	}

	/// <summary>
	/// Construct a Cluster within a World with an initial list of Blocks.
	/// </summary>
	public Cluster(World? world, List<Block> blocks) : this()
	{
		World = world;
		// Blocks = blocks;
		for (int i = 0; i < blocks.Count; i++)
		{
			AddBlock(blocks[i]);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (ControlMode == ControlMode.Player)
		{
			Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
			ApplyCentralForce(inputDirection * 500);
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState2D state)
	{
		base._IntegrateForces(state);
		for (int i = 0; i < state.GetContactCount(); i++)
		{
			int blockId = state.GetContactLocalShape(i);
			Block block = _blocks[blockId];
			block.Health -= 1;
			if (block.Health <= 0)
				DisableBlock(blockId);
		}
	}

	public override void _Draw()
	{
		base._Draw();

		if (World == null) return;
		Rid canvasItem = GetCanvasItem();
		switch (World.RenderMode)
		{
			case RenderMode.MultiMesh:

				foreach (ExpandableMultiMesh multiMesh in _expandableMultiMeshes.Values)
				{
					RenderingServer.CanvasItemAddMultimesh(canvasItem, multiMesh.MultiMeshRid);
				}
				break;
			case RenderMode.Canvas:
				for (int i = 0; i < _blocks.Count; i++)
				{
					Shape2D shape = ShapeOwnerGetShape(_shapeOwner, i);
					Transform2D blockTransform = _blocks[i].Transform;
					ShapeUtil.DrawShape(canvasItem, shape, new Color(1, 0, 0), blockTransform.Origin);
				}
				break;
			default:
				throw new ArgumentException("Enum has an invalid value.", nameof(World.RenderMode));
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		// Dispose of multi meshes when cluster object destroyed
		if (what == NotificationPredelete)
		{
			foreach (ExpandableMultiMesh multiMesh in _expandableMultiMeshes.Values)
				multiMesh.Dispose();
		}
	}

	/// <summary>
	/// Adds a new Block to the cluster.
	/// The block's BlockTypeID must be an ID that exists in this Cluster's World.
	/// </summary>
	public void AddBlock(Block block)
	{
		Debug.Assert(World != null, nameof(World) + " != null");
		
		int blockTypeId = block.BlockTypeId;
		if (blockTypeId > World.BlockTypes.Count)
		{
			throw new ArgumentException("The given block's BlockTypeID must be an ID that exists in the cluster's World.");
		}
		
		BlockType blockType = World.BlockTypes[blockTypeId];
		
		// Add a MultiMesh to the cluster if the BlockType hasn't been seen before
		if (_usedBlockTypes.Add(blockTypeId))
		{
			AddMultiMesh(blockTypeId, blockType.Mesh);
		}
		// Add the block's mesh
		ExpandableMultiMesh multiMesh = _expandableMultiMeshes[block.BlockTypeId];
		multiMesh.InstanceCount += 1;
		
		_blocks.Add(block);
		int blockId = _blocks.Count - 1;
		_expandableMultiMeshInstances[multiMesh][blockId] = multiMesh.InstanceCount - 1;
		
		// Add the block's shape to the cluster
		ShapeOwnerAddShape(_shapeOwner, blockType.Shape);
		PhysicsServer2D.BodySetShapeTransform(_rid, blockId, block.Transform);
		
		EnableBlock(blockId);
	}

	private void AddMultiMesh(int blockTypeId, Mesh mesh)
	{
		ExpandableMultiMesh multiMesh = new ExpandableMultiMesh(InitialMultiMeshBuffer);
		multiMesh.SetMesh(mesh);
		_expandableMultiMeshes.Add(blockTypeId, multiMesh);
		_expandableMultiMeshInstances[multiMesh] = new Dictionary<int, int>();
	}
	
	public void EnableBlock(int blockId)
	{
		Block block = _blocks[blockId];
		if (!block.Disabled)
			return;
		block.Disabled = false;
		
		// Enable collision shape
		PhysicsServer2D.BodySetShapeDisabled(_rid, blockId, false);
		
		// Show multi mesh instance
		ExpandableMultiMesh multiMesh = _expandableMultiMeshes[block.BlockTypeId];
		int multiMeshInstance = _expandableMultiMeshInstances[multiMesh][blockId];
		RenderingServer.MultimeshInstanceSetTransform2D(multiMesh.MultiMeshRid, multiMeshInstance, block.Transform);
		QueueRedraw();
	}

	public void DisableBlock(int blockId)
	{
		Block block = _blocks[blockId];
		if (block.Disabled)
			return;
		block.Disabled = true;
		
		// Disable collision shape
		CallDeferred(MethodName.ShapeSetDisabled, blockId, true);
		
		// Hide multi mesh instance
		ExpandableMultiMesh multiMesh = _expandableMultiMeshes[block.BlockTypeId];
		int multiMeshInstance = _expandableMultiMeshInstances[multiMesh][blockId];
		RenderingServer.MultimeshInstanceSetTransform2D(multiMesh.MultiMeshRid, multiMeshInstance, ZeroTransform);
		QueueRedraw();
	}

	private void ShapeSetDisabled(int shapeIdx, bool disabled)
	{
		PhysicsServer2D.BodySetShapeDisabled(_rid, shapeIdx, disabled);
	}
}

public enum ControlMode
{
	None,
	Player,
	Ai,
}