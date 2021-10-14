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
			public Vector3 PosB;
			public float Length;
			
			public static EdgeData CalculateForEdge(ProBuilderMesh pbMesh, Edge edge)
			{
				var a = pbMesh.positions[edge.a];
				var b = pbMesh.positions[edge.b];
				return new EdgeData
				{
					Edge = edge,
					PosA = a,
					PosB = b,
					Center = Math.Average(pbMesh.positions, new[] { edge.a, edge.b }),
					Length = (a - b).magnitude,
					Dir = (a - b).normalized
				};
			}

			public Vector3 GetCloserEdgePosition(Vector3 pos)
			{
				var aDist = Vector3.Distance(PosA, pos);
				var bDist = Vector3.Distance(PosB, pos);
				return aDist < bDist ? PosA : PosB;
			}
		}
}