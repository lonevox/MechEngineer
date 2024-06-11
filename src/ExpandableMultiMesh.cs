using System;
using System.Diagnostics;
using Godot;

namespace RapierTest;

/// <summary>
/// A partial C# reimplementation of MultiMesh that automatically expands the multi mesh buffer whenever InstanceCount would exceed it.
///
/// InstanceCount in ExpandableMultiMesh is slightly different to InstanceCount in MultiMesh. It doesn't clear and resize the buffer when
/// you change it unless you set it to a value that is too high for the current buffer. In that case, the buffer will be resized to fit
/// at least an InstanceCount number of instances, at which point the old buffer is copied into the new buffer. This means you don't lose
/// that data like you would in a regular MultiMesh.
/// InstanceCount must be set for the multi mesh to expand, so make sure to change it whenever you are adding instances.
/// </summary>
public class ExpandableMultiMesh : IDisposable
{
	public readonly Rid MultiMeshRid;
	public readonly RenderingServer.MultimeshTransformFormat TransformFormat;
	public readonly bool UsesColors;
	public readonly bool UsesCustomData;
	public readonly int MeshInstanceByteSize;
	
	private int _bufferSize;
	/// <summary>
	/// The size of the mesh instance buffer in bytes.
	/// </summary>
	public int BufferSize
	{
		get => _bufferSize;
		private set
		{
			if (value < _instanceCount * MeshInstanceByteSize)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "BufferSize must be greater than InstanceCount * MeshInstanceBytes.");
			}

			if (value == _bufferSize) return;
			if (value > 0)
			{
				float[] oldBuffer = RenderingServer.MultimeshGetBuffer(MultiMeshRid);
				float[] newBuffer = new float[value];
				if (_bufferSize > 0)
					Array.Copy(oldBuffer, newBuffer, oldBuffer.Length);
				int newInstanceCount = value / MeshInstanceByteSize * sizeof(float);
				RenderingServer.MultimeshAllocateData(MultiMeshRid, newInstanceCount, TransformFormat, UsesColors, UsesCustomData);
				RenderingServer.MultimeshSetBuffer(MultiMeshRid, newBuffer);
				_bufferSize = value;
			}
			else
			{
				RenderingServer.MultimeshAllocateData(MultiMeshRid, 0, RenderingServer.MultimeshTransformFormat.Transform2D);
				_bufferSize = 0;
			}
		}
	}
	private int _instanceCount;
	public int InstanceCount
	{
		get => _instanceCount;
		set
		{
			if (value * MeshInstanceByteSize > _bufferSize)
			{
				Expand(value);
			}
			_instanceCount = value;
		}
	}

	public ExpandableMultiMesh(RenderingServer.MultimeshTransformFormat transformFormat, bool usesColors, bool usesCustomData)
	{
		TransformFormat = transformFormat;
		UsesColors = usesColors;
		UsesCustomData = usesCustomData;
		
		// Calculate mesh instance byte size
		int meshInstanceFloatCount = 0;
		switch (TransformFormat)
		{
			case RenderingServer.MultimeshTransformFormat.Transform2D:
				meshInstanceFloatCount += 8;
				break;
			case RenderingServer.MultimeshTransformFormat.Transform3D:
				meshInstanceFloatCount += 12;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(transformFormat), "Enum has an invalid value.");
		}
		if (UsesColors)
			meshInstanceFloatCount += 4;
		if (UsesCustomData)
			meshInstanceFloatCount += 4;
		MeshInstanceByteSize = meshInstanceFloatCount * sizeof(float);
		
		MultiMeshRid = RenderingServer.MultimeshCreate();
	}
	
	/// <summary>
	/// Creates an expandable multi mesh with the given initial buffer.
	/// </summary>
	public ExpandableMultiMesh(
		float[] initialBuffer,
		RenderingServer.MultimeshTransformFormat transformFormat = RenderingServer.MultimeshTransformFormat.Transform2D,
		bool usesColors = false,
		bool usesCustomData = false) : this(transformFormat, usesColors, usesCustomData)
	{
		if (initialBuffer.Length % MeshInstanceByteSize != 0)
			throw new ArgumentOutOfRangeException(nameof(initialBuffer), "Buffer length must be divisible by the byte size of mesh instances.");

		// Set BufferSize without property side effects.
		_bufferSize = initialBuffer.Length * sizeof(float);

		int instanceCapacity = _bufferSize / MeshInstanceByteSize;
		RenderingServer.MultimeshAllocateData(MultiMeshRid, instanceCapacity, TransformFormat, UsesColors, UsesCustomData);
		RenderingServer.MultimeshSetBuffer(MultiMeshRid, initialBuffer);
	}

	/// <summary>
	/// Creates an expandable multimesh with a buffer size that can hold at least instanceCapacity instances.
	/// </summary>
	public ExpandableMultiMesh(
		int instanceCapacity,
		RenderingServer.MultimeshTransformFormat transformFormat = RenderingServer.MultimeshTransformFormat.Transform2D,
		bool usesColors = false,
		bool usesCustomData = false) : this(transformFormat, usesColors, usesCustomData)
	{
		Expand(instanceCapacity);
	}

	/// <summary>
	/// Sets the mesh to be drawn by the multimesh.
	/// </summary>
	public void SetMesh(Mesh mesh)
	{
		RenderingServer.MultimeshSetMesh(MultiMeshRid, mesh.GetRid());
	}

	/// <summary>
	/// Expand the multi mesh buffer to at least hold instanceCapacity number of instances.
	/// </summary>
	private void Expand(int instanceCapacity)
	{
		// Assert that the number of instances that can fit in the buffer is less than instanceCapacity. Only applicable with a capacity greater than 0.
		Debug.Assert(instanceCapacity > 0
			&& RenderingServer.MultimeshGetBuffer(MultiMeshRid).Length / MeshInstanceByteSize < instanceCapacity);

		int instanceCapacityBytes = instanceCapacity * MeshInstanceByteSize;
		int newBufferSize = _bufferSize * 2;

		// Allow the buffer to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when newBufferSize overflowed thanks to the (uint) cast.
        if ((uint)newBufferSize > Array.MaxLength)
			newBufferSize = Array.MaxLength;
		
		// If the computed capacity is still less than specified, set to the original argument.
		// Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
		if (newBufferSize < instanceCapacityBytes)
			newBufferSize = instanceCapacityBytes;

		BufferSize = newBufferSize;
	}
	
	private void ReleaseUnmanagedResources()
	{
		RenderingServer.FreeRid(MultiMeshRid);
	}

	/// <summary>
	/// Frees the multimesh RID in the RenderingServer.
	/// <i>Must</i> be called when the ExpandableMultiMesh is no longer needed, or else the multimesh will leak.
	/// </summary>
	public void Dispose()
	{
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~ExpandableMultiMesh()
	{
		ReleaseUnmanagedResources();
	}
}
