using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Math = UnityEngine.ProBuilder.Math;

namespace Tomi
{
	public class ConnectionPoint
	{
		public Vector3 Point { get; }
		public int MainRoadPointIndex { get; }
		public int MinorRoadPointIndex { get; }
		public SplineHandler MainRoad { get; }
		public SplineHandler MinorRoad { get; }
		public string MainRoadName { get; }
		public string MinorRoadName { get; }
		
		public Vector3 MainRoadDir => CalculateRoadDirection(MainRoad, MainRoadPointIndex);
		public Vector3 MinorRoadDir => CalculateRoadDirection(MinorRoad, MinorRoadPointIndex);
		
		private List<WingedEdge> _wingedEdges;
		public ConnectionPoint(Vector3 point, int mainIndex, int minorIndex, SplineHandler mainRoad, SplineHandler minorRoad)
		{
			Point = point;
			MainRoadPointIndex = mainIndex;
			MinorRoadPointIndex = minorIndex;
			MainRoad = mainRoad;
			MinorRoad = minorRoad;
			MainRoadName = mainRoad.Name;
			MinorRoadName = minorRoad.Name;
		}

		public ConnectionPoint(Vector3 point)
		{
			Point = point;
		}

		public void UpdateRoadConnectionsMesh()
		{
			_wingedEdges = BevelMainRoadConnection(out _);
			AdjustMinorRoadLength();
		}

		private struct EdgeData
		{
			public Edge Edge;
			public Vector3 Center;
			public Vector3 Dir;
			public float Length;
		}
		
		private EdgeData GetClosesEdge(SplineHandler splineHandler, Vector3 p)
		{
			var pbMesh = splineHandler.Builder.ProBuilderMesh;
			var closes = FindClosestEdgeToPoint(pbMesh, p);
			var edge = closes.Key;

			var aPos = pbMesh.positions[edge.a];
			var bPos = pbMesh.positions[edge.b];
			var edgeCenter = Math.Average(pbMesh.positions, new[] { edge.a, edge.b });
			var length = (aPos - bPos).magnitude;
			var dir = (edgeCenter - p).normalized;
			Debug.Log($"Closest edge on [{splineHandler.Builder.GameObject.name}] is {edge} length {length} dir {dir}");
			
			return new EdgeData()
			{
				Edge = edge,
				Center = edgeCenter,
				Length = length,
				Dir = dir,
			};
		}
		
		private List<WingedEdge> BevelMainRoadConnection(out EdgeData edgeData)
		{
			var pbMesh = MainRoad.Builder.ProBuilderMesh;
			edgeData = GetClosesEdge(MainRoad, Point);
			var newFace = Bevel.BevelEdges(pbMesh, new[] { edgeData.Edge }, edgeData.Length / 2);
			pbMesh.ToMesh();
			pbMesh.Refresh();
			return WingedEdge.GetWingedEdges(pbMesh, newFace);
		}

		private EdgeData AdjustMinorRoadLength()
		{
			var pbMesh = MinorRoad.Builder.ProBuilderMesh;
			var edgeData = GetClosesEdge(MinorRoad, Point);
			pbMesh.TranslateVertices(new[] { edgeData.Edge },MinorRoadDir * edgeData.Length / 2);
			pbMesh.ToMesh();
			pbMesh.Refresh();
			return edgeData;
		}
		private KeyValuePair<Edge, Vector3> FindClosestEdgeToPoint(ProBuilderMesh mesh, Vector3 point)
		{
			var orderedList = new Dictionary<Edge, float>();

			foreach (var face in mesh.faces)
			{
				foreach (var edge in face.edges)
				{
					var edgeCenter = Math.Average(mesh.positions, new[] { edge.a, edge.b });
					var dist = Vector3.Distance(point, edgeCenter);
					orderedList.Add(edge, dist);
				}
			}

			if (orderedList.Count > 0)
			{
				var orderedEnumerable = orderedList.OrderBy(o => o.Value).ToArray();
				var first = orderedEnumerable[0];

				var pos = Math.Average(mesh.positions, new[] { first.Key.a, first.Key.b });
				return new KeyValuePair<Edge, Vector3>(first.Key,pos);
			}

			throw new Exception($"No faces in mesh {mesh}");
		}

		public Vector3 CalculateRoadDirection(SplineHandler road, int index)
		{
			var dir = road.Points[0] - road.Points[index];
			
			if (index > 0 && index < road.Points.Count - 1)
				dir = road.Points[index + 1] - road.Points[index];

			if (index == 0)
				dir = road.Points[1] - road.Points[0];
			
			return dir.normalized;
		}
	}
}