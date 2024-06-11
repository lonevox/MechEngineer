using System.Collections.Generic;
using Godot;
using RapierTest.Util;

namespace RapierTest;

public partial class World : Node2D
{
	/// <summary>
	/// This list is the authority on the available BlockTypes. The index of each BlockType is its ID.
	/// </summary>
	public readonly List<BlockType> BlockTypes = new();
	
	[Export]
	public RenderMode RenderMode { get; set; }

	public World()
	{
		RenderMode = RenderMode.MultiMesh;
	}

	public override void _Ready()
	{
		base._Ready();
		GD.Print("creating world");
		RectangleShape2D smallRectangle = new RectangleShape2D();
		smallRectangle.Size = new Vector2(5, 5);
		AddBlockType(new BlockType("Command", smallRectangle, 10, 1, 0.5f));
		Vector2[] smallTrianglePolygon = { Vector2.Zero, new(5, 0), new(5, 5) };
		AddBlockType(new BlockType("Small TriHull", new ConvexPolygonShape2D { Points = smallTrianglePolygon }, 10, 1, 0.5f));
		Vector2[] mediumTrianglePolygon = { Vector2.Zero, new(20, 0), new(20, 20) };
		AddBlockType(new BlockType("Medium TriHull", new ConvexPolygonShape2D { Points = mediumTrianglePolygon }, 50, 2, 0.5f));
		AddBlockType(new BlockType("Circle", new ConvexPolygonShape2D() { Points = PolygonUtil.CirclePolygon(2.5f, 4) }, 5, 2, 0.5f));

		Cluster cluster = new Cluster();
		AddCluster(cluster);
		cluster.ControlMode = ControlMode.Player;
		float rotation = 0;
		for (int i = 0; i < 150; i++)
		{
			for (int j = 0; j < 100; j++)
			{
				rotation += 0.1f;
				cluster.AddBlock(Block.FromWorldBlockType(0, this, new Transform2D(rotation, new Vector2(i * 5, j * 5))));
			}
		}
	}

	public void AddBlockType(BlockType blockType)
	{
		BlockTypes.Add(blockType);
	}

	private void AddCluster(Cluster cluster)
	{
		cluster.World = this;
		AddChild(cluster);
	}
}

public enum RenderMode
{
	MultiMesh,
	Canvas,
}
