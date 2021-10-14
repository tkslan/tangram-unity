using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Math = UnityEngine.ProBuilder.Math;

namespace Tomi
{
	public partial class ConnectionPoint
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
		
			var pbMeshMain = MainRoad.Builder.ProBuilderMesh;
			var pbMeshMinor = MinorRoad.Builder.ProBuilderMesh;
			
			_wingedEdges = BevelMainRoadConnection(out var mainEdge);
			if(_wingedEdges.Count == 0) return;
			
			var e = _wingedEdges.Find(f => f.opposite == null);
			var we = EdgeData.CalculateForEdge(pbMeshMain,e.edge.local);
			var edgeData = AdjustMinorRoadLength();
			
			var dot = GetDot(edgeData, mainEdge);

			var absDot = Mathf.Abs(dot);
			if (absDot > 0.4f)
			{
				//Move back
				pbMeshMain.TranslateVertices(new[] { e.edge.local }, edgeData.Dir * -0.3f);
				var ed = EdgeData.CalculateForEdge(pbMeshMain, e.next.edge.local);
				
				pbMeshMain.ToMesh();
				pbMeshMain.Refresh();
				
				
				var closestEdgeInMainRoad = FindClosestEdgeToPoint(pbMeshMain, edgeData.Center);
				Debug.Log($"Closes in main: {closestEdgeInMainRoad.Key}");
				var mc = EdgeData.CalculateForEdge(pbMeshMain, closestEdgeInMainRoad.Key);
				pbMeshMinor.TranslateVertices(new[]{edgeData.Edge.a},   edgeData.GetCloserEdgePosition(mc.PosA) -mc.PosA);
				pbMeshMinor.TranslateVertices(new[]{edgeData.Edge.b},   edgeData.GetCloserEdgePosition(mc.PosB) -mc.PosB);
				pbMeshMinor.ToMesh();
				pbMeshMinor.Refresh();
			}
			
			var newMainMesh = CombineMeshes.Combine(new[] { pbMeshMain, pbMeshMinor }, pbMeshMain)[0];

			newMainMesh.ToMesh();
			newMainMesh.Refresh();
			
			if (newMainMesh.faces.Count > 0)
			{
				MinorRoad.Invalidate();
				GameObject.DestroyImmediate(MinorRoad.Builder.GameObject);
				newMainMesh.WeldVertices(newMainMesh.faces.SelectMany(x => x.indexes), 0.25f);
				newMainMesh.ToMesh();
				newMainMesh.Refresh();
			}
		}

		private void Merge(ProBuilderMesh mesh, int[] indexes)
		{
			if (indexes.Length > 1)
			{
				int newIndex = mesh.MergeVertices(indexes, true);

				var success = newIndex > -1;

				if (success)
					mesh.SetSelectedVertices(new int[] { newIndex });

				mesh.ToMesh();
				mesh.Refresh();
			}
		}

		private EdgeData GetClosesEdge(SplineHandler splineHandler, Vector3 p)
		{
			var pbMesh = splineHandler.Builder.ProBuilderMesh;
			var closes = FindClosestEdgeToPoint(pbMesh, p);
			return EdgeData.CalculateForEdge(pbMesh,closes.Key);
		}
		
		private float GetDot(EdgeData lhs, EdgeData rhs)
		{
			var dot = Vector3.Dot(lhs.Dir, rhs.Dir);
			Debug.Log($"DOT [{MainRoad.Name}]->[{MinorRoad.Name}]={dot}");
			return dot;
		}

		private List<WingedEdge> BevelMainRoadConnection(out EdgeData edgeData)
		{
			var wingedEdges = new List<WingedEdge>();
			var pbMesh = MainRoad.Builder.ProBuilderMesh;
			edgeData = GetClosesEdge(MainRoad, Point);
			if (pbMesh == null)
			{
				Debug.LogError($"Not pbMesh created for [{MainRoad.Name}]");
				return wingedEdges;
			}
			var newFace = Bevel.BevelEdges(pbMesh, new[] { edgeData.Edge }, edgeData.Length / 2);
			if (newFace?.Count > 0)
			{
				wingedEdges = WingedEdge.GetWingedEdges(pbMesh, newFace);
				pbMesh.ToMesh();
				pbMesh.Refresh();
			}
			
			return wingedEdges;
		}

		private EdgeData AdjustMinorRoadLength()
		{
			var pbMesh = MinorRoad.Builder.ProBuilderMesh;
			var edgeData = GetClosesEdge(MinorRoad, Point);
			pbMesh.TranslateVertices(new[] { edgeData.Edge }, MinorRoadDir * edgeData.Length * 0.5f);
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