using Godot;
using RapierTest.Util;

namespace RapierTest;

/// <summary>
/// 
/// </summary>
/// <param name="Name"></param>
/// <param name="Shape"></param>
/// <param name="Mesh"></param>
/// <param name="PortLocations"></param>
/// <param name="Scale"></param>
/// <param name="Durability"></param>
/// <param name="Density"></param>
/// <param name="Mass"></param>
/// <param name="Area"></param>
/// <param name="Health"></param>
/// <param name="Weak">Weak blocks are destroyed when one of their neighbours is destroyed.</param>
public sealed record BlockType(string Name, Shape2D Shape, Mesh Mesh, Vector2[] PortLocations, int Scale, float Durability, float Density, float Mass, float Area, float Health, bool Weak)
{
	public static BlockTypeBuilder Builder(string name, Shape2D shape)
	{
		return new BlockTypeBuilder(name, shape);
	}
	
	public class BlockTypeBuilder
	{
		private readonly string _name;
		private readonly Shape2D _shape;
		private int _scale = 1;
		private float _durability;
		private float _density;
		private float _mass;
		private float _health;
		private Vector2[]? _portLocations;
		private bool _weak;
		
		public BlockTypeBuilder(string name, Shape2D shape)
		{
			_name = name;
			_shape = shape;
		}
		
		public BlockTypeBuilder PortLocations(Vector2[] portLocations)
		{
			_portLocations = portLocations;
			return this;
		}

		public BlockTypeBuilder Scale(int scale)
		{
			if (scale < 1)
				throw new ArgumentOutOfRangeException(nameof(scale), "BlockType scale must be 1 or more.");
			_scale = scale;
			return this;
		}
		
		public BlockTypeBuilder Durability(float durability)
		{
			if (durability <= 0)
				throw new ArgumentOutOfRangeException(nameof(durability), "BlockType durability must be greater than 0.");
			_durability = durability;
			return this;
		}
		
		public BlockTypeBuilder Density(float density)
		{
			if (density <= 0)
				throw new ArgumentOutOfRangeException(nameof(density), "BlockType density must be greater than 0.");
			_density = density;
			return this;
		}
		
		public BlockTypeBuilder Mass(float mass)
		{
			if (mass <= 0)
				throw new ArgumentOutOfRangeException(nameof(mass), "BlockType mass must be greater than 0.");
			_mass = mass;
			return this;
		}
		
		public BlockTypeBuilder Health(float health)
		{
			if (health <= 0)
				throw new ArgumentOutOfRangeException(nameof(health), "BlockType health must be greater than 0.");
			_health = health;
			return this;
		}
		
		public BlockTypeBuilder Weak()
		{
			_weak = true;
			return this;
		}

		public BlockType Build()
		{
			Mesh mesh = ShapeUtil.Shape2DToMesh(_shape);
			
			if (_portLocations == null)
			{
				// TODO: Generate port locations from shape
				_portLocations = Array.Empty<Vector2>();
			}
			
			float area = ShapeUtil.Shape2DArea(_shape);
			
			// If density or mass is missing, then one is used to specify the other. If both are missing, throw.
			if (_density != 0 && _mass == 0)
				_mass = _density * area;
			else if (_density == 0 && _mass != 0)
				_density = _mass / area;
			else
				throw new Exception("Must specify either density or mass in order to build BlockType.");
			
			// If durability is specified, overwrite health. Otherwise, health sets durability. If health isn't set either, throw.
			if (_durability != 0)
				_health = _durability * area;
			else if (_health != 0)
				_durability = _health / area;
			else
				throw new Exception("Must specify either durability or health in order to build BlockType.");
			
			return new BlockType(_name, _shape, mesh, _portLocations, _scale, _durability, _density, _mass, area, _health, _weak);
		}
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
