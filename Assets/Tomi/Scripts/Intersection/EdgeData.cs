using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi
{
		public struct EdgeData
		{
			public Edge Edge;
			public Vector3 Center;
			public Vector3 Dir;
			public Vector3 PosA;
			public Vector3 PosBWorld;
			public Vector3 PosAWorld;
			public Vector3 PosB;
			public float Length;
			
			public static EdgeData CalculateForEdge(ProBuilderMesh pbMesh, Edge edge)
			{
				var a = pbMesh.positions[edge.a];
				var b = pbMesh.positions[edge.b];
				var wa = GetWorldPosition(pbMesh, edge.a);
				var wb = GetWorldPosition(pbMesh, edge.b);
				return new EdgeData
				{
					Edge = edge,
					PosA = a,
					PosB= b,
					Center = Math.Average(pbMesh.positions, new[] { edge.a, edge.b }),
					Length = (a - b).magnitude,
					Dir = (a - b).normalized,
					PosAWorld = wa,
					PosBWorld = wb,
				};
			}

			public Vector3 GetCloserEdgePosition(Vector3 pos)
			{
				var aDist = Vector3.Distance(PosA, pos);
				var bDist = Vector3.Distance(PosBWorld, pos);
				return aDist < bDist ? PosA : PosBWorld;
			}
			public Vector3 GetCloserEdgePositionWorld(Vector3 pos)
			{
				var aDist = Vector3.Distance(PosAWorld, pos);
				var bDist = Vector3.Distance(PosB, pos);
				return aDist < bDist ? PosA : PosBWorld;
			}
			private static Vector3 GetWorldPosition(ProBuilderMesh mesh, int index)
			{
				var position = mesh.positions[index];

				var l2w = mesh.transform.localToWorldMatrix;

				return l2w.MultiplyPoint3x4(position);
			}
		}
}