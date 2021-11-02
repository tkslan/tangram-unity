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

		public List<WingedEdge> WingedEdges => _wingedEdges;
		private List<WingedEdge> _wingedEdges;
		private EdgeData _bevelPointEdgeData;
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

		public void BevelMainRoadConnection()
		{
			if (_wingedEdges == null || _wingedEdges.Count == 0)
				_wingedEdges = BevelMainRoadPoint();
			
			if (_wingedEdges == null)
				throw new NotSupportedException("Error on bevel");
		}
		
		public void UpdateRoadConnectionsMesh()
		{
			var pbMeshMain = MainRoad.Builder.ProBuilderMesh;
			var pbMeshMinor = MinorRoad.Builder.ProBuilderMesh;
			
			RemoveOverlappedFaces();
			var edgeData = AdjustMinorRoadLength();
			
			//Adjust angled connection edges
			_bevelPointEdgeData = GetClosesEdge(MainRoad, Point);
			var dot = GetDot(edgeData, _bevelPointEdgeData);
			var mc = ReturnClosestEdgeOnMesh(pbMeshMain,edgeData.Center);
			//Adjust angled connections by offsetting winded edge bit back
			if (Mathf.Abs(dot) > 0.1 && Mathf.Abs(dot) < 0.9)
			{
				var e = _wingedEdges.Find(f => f.opposite == null);
				var edge = e != null ? e.edge.local : mc.Edge;
				var offset = mc.Dir * (mc.Length / 3);
				pbMeshMain.TranslateVertices(new[] { edge }, offset);
				
				//Make more space when connection is to narrow
				if (mc.Length < 1f)
				{
					pbMeshMain.TranslateVertices(new [] {mc.Edge.a}, offset / 1.5f);
					pbMeshMain.TranslateVertices(new [] {mc.Edge.b}, -offset / 1.5f);
				}
			}
			
			
			//Snap vertexes
			var mainVertA = pbMeshMain.VerticesInWorldSpace()[mc.Edge.b];
			var mainVertB = pbMeshMain.VerticesInWorldSpace()[mc.Edge.a];
			var vert = pbMeshMinor.GetVertices();
			vert[edgeData.Edge.a].position = mainVertA;
			vert[edgeData.Edge.b].position = mainVertB;
			pbMeshMinor.SetVertices(vert);
			pbMeshMinor.ToMesh();

			//Combine to single mesh
			var mesh = CombineMeshes.Combine(new[] { pbMeshMain, pbMeshMinor }, pbMeshMain);
			var first = mesh[0];
			first.WeldVertices(mesh[0].faces.SelectMany(s => s.indexes), 0.2f);
			first.ToMesh(MeshTopology.Quads);
			first.Refresh();
			
			GameObject.DestroyImmediate(MinorRoad.Builder.GameObject);
		}

		
		private EdgeData ReturnClosestEdgeOnMesh(ProBuilderMesh pbMesh, Vector3 point)
		{
			var closestEdgeInMainRoad = FindClosestEdgeToPoint(pbMesh, point);
			Debug.Log($"Closes in main: {closestEdgeInMainRoad.Key}");
			var mc = EdgeData.CalculateForEdge(pbMesh, closestEdgeInMainRoad.Key);
			return mc;
		}
		private void Merge(ProBuilderMesh mesh, int[] indexes)
		{
			if (indexes.Length > 1)
			{
				int newIndex = mesh.MergeVertices(indexes);

				var success = newIndex > -1;

				if (success)
					mesh.SetSelectedVertices(new int[] { newIndex });
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

		private List<WingedEdge> BevelMainRoadPoint()
		{
			var wingedEdges = new List<WingedEdge>();
			var pbMesh = MainRoad.Builder.ProBuilderMesh;
			
			var edgeData = GetClosesEdge(MainRoad, Point);
			if (pbMesh == null)
			{
				Debug.LogError($"Not pbMesh created for [{MainRoad.Name}]");
				return wingedEdges;
			}
			
			var newFace = Bevel.BevelEdges(pbMesh, new[] { edgeData.Edge }, edgeData.Length / 2);
			
			if (newFace == null)
			{
				Debug.LogError(edgeData.Edge);
				return wingedEdges;
			}
			
			Debug.Log("Bevel done: "+ edgeData.Edge);
			wingedEdges = WingedEdge.GetWingedEdges(pbMesh, newFace);
			pbMesh.ToMesh();
			pbMesh.Refresh();

			return wingedEdges;
		}

		private void RemoveOverlappedFaces()
		{
			var pbMinor = MinorRoad.Builder.ProBuilderMesh;
			var p2d = new Vector2(Point.x, Point.z);
			var facesToCheck = FindClosestFaceToPoint(pbMinor, Point);
			if (facesToCheck == null) return;
			
			foreach (var pair in facesToCheck)
			{
				//Use 2d vector, Y pos is different
				var v2d = new Vector2(pair.Value.x, pair.Value.z);
				if (Vector2.Distance(v2d, p2d) < 1f)
				{
					pbMinor.DeleteFace(pair.Key);
					pbMinor.ToMesh();
					Debug.LogWarning($"Removed face:{pair.Key}");
				}
			}
		}
		private EdgeData AdjustMinorRoadLength()
		{
			var pbMesh = MinorRoad.Builder.ProBuilderMesh;
			var edgeData = GetClosesEdge(MinorRoad, Point);
			if (MainRoad.Name.Equals("616321508"))
			{
				Debug.Log(MinorRoadDir);
				pbMesh.TranslateVertices(new[] { edgeData.Edge }, -MinorRoadDir * (edgeData.Length / 2));
			}
			pbMesh.TranslateVertices(new[] { edgeData.Edge }, MinorRoadDir * (edgeData.Length / 2));
			pbMesh.ToMesh(MeshTopology.Quads);
			pbMesh.Refresh();
			MinorRoad.Invalidate();
			//Return new position after translation
			return GetClosesEdge(MinorRoad, Point);
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

		private List<KeyValuePair<Face, Vector3>> FindClosestFaceToPoint(ProBuilderMesh mesh, Vector3 point)
		{
			var orderedList = new Dictionary<Face, Vector3>();
			//Don't use on to small objects
			if (mesh.faceCount <= 2)
				return null;	
			
			foreach (var face in mesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(mesh.positions, new[] {edge.a, edge.b})).ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos);
			}

			if (orderedList.Count > 0)
			{
				var orderedEnumerable = orderedList.OrderBy(o => Vector3.Distance(o.Value, point));
				return orderedEnumerable.ToList();
			}

			return null;
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