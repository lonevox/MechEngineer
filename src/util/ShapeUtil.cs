using System;
using Godot;

namespace RapierTest.Util;

public static class ShapeUtil
{
	/// <summary>
	/// Only works with RectangleShape2D, ConvexPolygonShape2D, or ConcavePolygonShape2D.
	/// </summary>
	public static Mesh Shape2DToMesh(Shape2D shape)
	{
		switch (shape.GetClass())
		{
			case "RectangleShape2D":
				return RectangleShape2DToMesh((shape as RectangleShape2D)!);
			case "ConvexPolygonShape2D":
				ConvexPolygonShape2D convexShape = (shape as ConvexPolygonShape2D)!;
				return PolygonUtil.ConvexPolygonToMesh(convexShape.Points);
			case "ConcavePolygonShape2D":
				ConcavePolygonShape2D concaveShape = (shape as ConcavePolygonShape2D)!;
				return PolygonUtil.PolygonToMesh(concaveShape.Segments);
			default:
				throw new ArgumentException("Invalid Shape2D type: The given shape is of type '" + shape.GetClass() +
					"'. The only valid types are 'RectangleShape2D', 'ConvexPolygonShape2D', and 'ConcavePolygonShape2D'.");
		}
	}

	public static Mesh CircleShape2DToMesh(CircleShape2D shape, int radialSegments)
	{
		Vector2[] polygon = PolygonUtil.CirclePolygon(shape.Radius, radialSegments);
		return PolygonUtil.PolygonToMesh(polygon);
	}

	public static Mesh RectangleShape2DToMesh(RectangleShape2D shape)
	{
		return new QuadMesh
		{
			Size = shape.Size
		};
	}

	public static Mesh ConvexPolygonShape2DToMesh(ConvexPolygonShape2D shape)
	{
		return PolygonUtil.PolygonToMesh(shape.Points);
	}

	public static Mesh ConcavePolygonShape2DToMesh(ConcavePolygonShape2D shape)
	{
		Vector2[] polygon = PolygonUtil.PolygonSegmentsToPoints(shape.Segments);
		return PolygonUtil.PolygonToMesh(polygon);
	}

	public static void DrawShape(Rid canvasItem, Shape2D shape, Color color, Vector2 position)
	{
		switch (shape.GetClass())
		{
			case "RectangleShape2D":
				DrawRectangleShape(canvasItem, (shape as RectangleShape2D)!, color, position);
				break;
			case "ConvexPolygonShape2D":
				DrawConvexPolygonShape(canvasItem, (shape as ConvexPolygonShape2D)!, color, position);
				break;
			case "ConcavePolygonShape2D":
				DrawConcavePolygonShape(canvasItem, (shape as ConcavePolygonShape2D)!, color, position);
				break;
			default:
				throw new ArgumentException("Invalid Shape2D type: The given shape is of type '" + shape.GetClass() +
					"'. The only valid types are 'RectangleShape2D', 'ConvexPolygonShape2D', and 'ConcavePolygonShape2D'.");
		}
	}

	public static void DrawRectangleShape(Rid canvasItem, RectangleShape2D shape, Color color, Vector2 position)
	{
		Rect2 rect = new Rect2(position, shape.Size);
		RenderingServer.CanvasItemAddRect(canvasItem, rect, color);
	}

	public static void DrawConvexPolygonShape(Rid canvasItem, ConvexPolygonShape2D shape, Color color, Vector2 position)
	{
		PolygonUtil.DrawPolygonAtPosition(canvasItem, shape.Points, color, position);
	}

	public static void DrawConcavePolygonShape(Rid canvasItem, ConcavePolygonShape2D shape, Color color, Vector2 position)
	{
		Vector2[] polygon = PolygonUtil.PolygonSegmentsToPoints(shape.Segments);
		PolygonUtil.DrawPolygonAtPosition(canvasItem, polygon, color, position);
	}

	public static float Shape2DArea(Shape2D shape)
	{
		switch (shape.GetClass())
		{
			case "RectangleShape2D":
				return RectangleShape2DArea((shape as RectangleShape2D)!);
			case "ConvexPolygonShape2D":
				return ConvexPolygonShape2DArea((shape as ConvexPolygonShape2D)!);
			case "ConcavePolygonShape2D":
				return ConcavePolygonShape2DArea((shape as ConcavePolygonShape2D)!);
			default:
				throw new ArgumentException("Invalid Shape2D type: The given shape is of type '" + shape.GetClass() +
				                            "'. The only valid types are 'RectangleShape2D', 'ConvexPolygonShape2D', and 'ConcavePolygonShape2D'.");
		}
	}

	public static float RectangleShape2DArea(RectangleShape2D shape)
	{
		return shape.Size.X * shape.Size.Y;
	}
	
	public static float ConvexPolygonShape2DArea(ConvexPolygonShape2D shape)
	{
		return PolygonUtil.PolygonToArea(shape.Points);
	}
	
	public static float ConcavePolygonShape2DArea(ConcavePolygonShape2D shape)
	{
		return PolygonUtil.PolygonToArea(PolygonUtil.PolygonSegmentsToPoints(shape.Segments));
	}
}
