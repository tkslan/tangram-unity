using System.Collections.Generic;
using Tomi.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi.Geometry
{
	public abstract class Geometry
	{
		protected readonly ProBuilderMesh PbMesh;
		public readonly List<Vector2> Points;
		public Geometry(ProBuilderMesh mesh, List<Vector3> points)
		{
			PbMesh = mesh;
			Points = new List<Vector2>();
			foreach (var point in points)
			{
				Points.Add(point.ToVector2());
			}
		}

		public void TranslateEdge(EdgeData edgeData, Vector2 offset)
		{
			var offsetV3 = new Vector3(offset.x, 0, offset.y);
			PbMesh.TranslateVertices(new [] {edgeData.Edge}, offsetV3);
		}
		public void TranslateFace(Face face, Vector2 offset)
		{
			var offsetV3 = new Vector3(offset.x, 0, offset.y);
			PbMesh.TranslateVertices(new [] {face}, offsetV3);
		}
		public abstract void Refresh();
	}
}