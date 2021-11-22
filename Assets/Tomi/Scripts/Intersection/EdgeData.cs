using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi
{
		public struct EdgeData
		{
			public Edge Edge;
			public Vector2 Center;
			public Vector2 Dir;
			public Vector2 PosA;
			public Vector2 PosB;
			public float Length;
			public bool Internal;

			public EdgeData(ProBuilderMesh pbMesh, Edge edge)
			{
				Edge = edge;
				PosA = pbMesh.positions[edge.a].ToVector2();
				PosB = pbMesh.positions[edge.b].ToVector2();
				Center = Math.Average(pbMesh.positions, new[] {edge.a, edge.b}).ToVector2();
				Length = (PosA - PosB).magnitude;
				Dir = (PosA - PosB).normalized;
				Internal = false;
			}
			public static EdgeData CalculateForEdge(ProBuilderMesh pbMesh, Edge edge)
			{
				return new EdgeData(pbMesh, edge);
			}
			public void CheckIsInternal(List<Vector3> points)
			{
				var center = Center;
				var any = points.Any(a => Vector2.Distance(a.ToVector2(), center) < 0.01f);
				Internal = any;
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