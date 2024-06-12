using System;
using Godot;

namespace MechGrinder.Util;

public static class PolygonUtil
{
	public static Vector2[] PolygonSegmentsToPoints(Vector2[] segments)
	{
		Vector2[] points = new Vector2[segments.Length / 2];
		for (int i = 0; i < points.Length; i++)
			points[i] = segments[i * 2];
		return points;
	}

	public static Vector2[] PolygonToTriangleVertices(Vector2[] polygon)
	{
		int[] triangleIndices = Geometry2D.TriangulatePolygon(polygon);
		return PolygonVerticesAtIndices(polygon, triangleIndices);
	}

	/// <summary>
	/// Creates a mesh from a polygon. If you are sure that the polygon is convex, prefer <see cref="ConvexPolygonToMesh"/>
	/// as it creates a smaller mesh.
	/// </summary>
	public static Mesh PolygonToMesh(Vector2[] polygon)
	{
		Vector2[] triangleVertices = PolygonToTriangleVertices(polygon);
		return BasicMesh(Mesh.PrimitiveType.Triangles, triangleVertices);
	}

	public static Mesh ConvexPolygonToMesh(Vector2[] polygon)
	{
		Vector2[] triangleStripVertices = ConvexPolygonTriangleStripVertices(polygon);
		return BasicMesh(Mesh.PrimitiveType.TriangleStrip, triangleStripVertices);
	}

	/// <summary>
	/// Creates a basic mesh. It has a single surface which is rendered with the given primitive type and vertex array.
	/// </summary>
	private static Mesh BasicMesh(Mesh.PrimitiveType primitive, Vector2[] vertices)
	{
		Godot.Collections.Array meshArray = new Godot.Collections.Array();
		meshArray.Resize((int)Mesh.ArrayType.Max);
		meshArray[(int)Mesh.ArrayType.Vertex] = vertices;

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(primitive, meshArray);
		return mesh;
	}

	public static Vector2[] RegularConvexPolygon(int sides, float radius)
	{
		if (sides < 3)
			throw new ArgumentException("Regular convex polygon must have 3 or more sides.");
		
		float angleDelta = MathF.PI * 2 / sides;
		Vector2 vector = new Vector2(radius, 0);
		Vector2[] polygon = new Vector2[sides];
		for (int i = 0; i < sides; i++)
		{
			polygon[i] = vector;
			vector = vector.Rotated(angleDelta);
		}
		return polygon;
	}

	public static int[] ConvexPolygonTriangleStripIndices(Vector2[] polygon)
	{
		int[] triangleStripIndices = new int[polygon.Length];
		for (int i = 0; i < triangleStripIndices.Length; i++)
		{
			if (i % 2 == 0) // If even
				triangleStripIndices[i] = i / 2;
			else
				triangleStripIndices[i] = triangleStripIndices.Length - (i + 1) / 2;
		}
		return triangleStripIndices;
	}

	public static Vector2[] ConvexPolygonTriangleStripVertices(Vector2[] polygon)
	{
		int[] triangleStripIndices = ConvexPolygonTriangleStripIndices(polygon);
		return PolygonVerticesAtIndices(polygon, triangleStripIndices);
	}

	public static Vector2[] PolygonVerticesAtIndices(Vector2[] polygon, int[] indices)
	{
		Vector2[] vertices = new Vector2[indices.Length];
		for (int i = 0; i < vertices.Length; i++)
			vertices[i] = polygon[indices[i]];
		return vertices;
	}

	public static Vector2[] TranslatePolygon(Vector2[] polygon, Vector2 translation)
	{
		Vector2[] polygonTranslated = new Vector2[polygon.Length];
		for (int j = 0; j < polygon.Length; j++)
		{
			Vector2 point = polygon[j];
			Vector2 pointTranslated = point + translation;
			polygonTranslated[j] = pointTranslated;
		}
		return polygonTranslated;
	}

	public static void DrawPolygonAtPosition(Rid canvasItem, Vector2[] polygon, Color color, Vector2 position)
	{
		Vector2[] polygonTranslated = TranslatePolygon(polygon, position);
		RenderingServer.CanvasItemAddPolygon(canvasItem, polygonTranslated, new[] { color });
	}
	
	public static float TriangleToArea(Vector2 a, Vector2 b, Vector2 c)
	{
		return 0.5f * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
	}

	public static float PolygonToArea(Vector2[] polygon)
	{
		Vector2[] triangleVertices = PolygonToTriangleVertices(polygon);
		float area = 0;
		for (int i = 0; i < triangleVertices.Length / 3; i++)
		{
			area += TriangleToArea(triangleVertices[i], triangleVertices[i + 1], triangleVertices[i + 2]);
		}
		return area;
	}
}