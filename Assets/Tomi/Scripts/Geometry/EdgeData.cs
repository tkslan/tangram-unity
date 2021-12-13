using System.Collections.Generic;
using System.Linq;
using Tomi.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi.Geometry
{
	public struct EdgeData
	{
			public Edge Edge { get; }
			public Vector2 Center { get; }
			public Vector2 Dir { get; }
			public Vector2 PosA { get; }
			public Vector2 PosB { get; }
			public float Length { get; }
			public bool Internal { get; private set; }
			public int Index { get; private set; }
			public bool Valid { get; }
			public EdgeData(ProBuilderMesh pbMesh, Edge edge)
			{
				Edge = edge;
				PosA = pbMesh.positions[edge.a].ToVector2();
				PosB = pbMesh.positions[edge.b].ToVector2();
				Center = Math.Average(pbMesh.positions, new[] {edge.a, edge.b}).ToVector2();
				Length = (PosA - PosB).magnitude;
				Dir = (PosA - PosB).normalized;
				Internal = false;
				Index = -1;
				Valid = edge.a != edge.b;
			}
			public static EdgeData CalculateForEdge(ProBuilderMesh pbMesh, Edge edge)
			{
				return new EdgeData(pbMesh, edge);
			}
			public void CheckIsInternal(List<Vector3> points)
			{
				var margin = 0.2f;
				var center = Center;
				
				var findIndex = points.FindIndex(f => Vector2.Distance(f.ToVector2(), center) < margin);
				
				//Don't include caps (first and last point) as internal
				if(findIndex > 0 && findIndex < points.Count - 1)
					Internal = true;
				//But only set index to mark them
				if(findIndex >= 0)
					Index = findIndex;
			}
			public Vector2 GetCloserEdgePosition(Vector2 pos)
			{
				var aDist = Vector2.Distance(PosA, pos);
				var bDist = Vector2.Distance(PosB, pos);
				return aDist < bDist ? PosA : PosB;
			}
			private static Vector2 GetWorldPosition(ProBuilderMesh mesh, int index)
			{
				return ToWorldPos(mesh, new Vector2(mesh.positions[index].x,mesh.positions[index].z));
			}

			public static Vector2 ToWorldPos(ProBuilderMesh mesh, Vector2 pos)
			{
				var position = pos;

				var l2w = mesh.transform.localToWorldMatrix;

				return l2w.MultiplyPoint3x4(position);
			}
		}
}