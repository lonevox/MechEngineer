using Godot;
using RapierTest.Util;

namespace RapierTest;

public class BlockType
{
	public readonly string Name;
	public readonly Shape2D Shape;
	public readonly Mesh Mesh;
	public readonly int Scale;
	public readonly float Durability;
	public readonly float Density;
	public readonly float Area;
	public readonly float Mass;
	public readonly float Health;
	/// <summary>
	/// Weak blocks are destroyed when one of their neighbours is destroyed.
	/// </summary>
	public readonly bool Weak;

	public BlockType(string name, Shape2D shape, int scale = 1, float durability = 1, float density = 1, bool weak = false)
	{
		Name = name;
		Shape = shape;
		Scale = scale;
		Durability = durability;
		Density = density;
		Weak = weak;
		
		Mesh = ShapeUtil.Shape2DToMesh(Shape);
		Area = ShapeUtil.Shape2DArea(Shape);
		Mass = density * Area;
		Health = durability * Mass;
	}
}

// TODO: Turn this class into a struct.
public class Block
{
	public int BlockTypeId;
	public Transform2D Transform;
	public float Health;
	public bool Disabled = true;

	public Block(int blockTypeId, Transform2D transform, float health)
	{
		BlockTypeId = blockTypeId;
		Transform = transform;
		Health = health;
	}

	public static Block FromWorldBlockType(int blockTypeId, World world)
	{
		return FromWorldBlockType(blockTypeId, world, Transform2D.Identity);
	}

	public static Block FromWorldBlockType(int blockTypeId, World world, Transform2D transform)
	{
		BlockType blockType = world.BlockTypes[blockTypeId];
		return new Block(blockTypeId, transform, blockType.Health);
	}
}
