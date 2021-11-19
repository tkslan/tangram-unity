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

		public List<WingedEdge> WingedEdges => _wingedEdges;
		private List<WingedEdge> _wingedEdges;
		private EdgeData _bevelPointEdgeData;
		public ConnectionPoint(Vector3 point, int mainIndex, int minorIndex, SplineHandler mainRoad, SplineHandler minorRoad)
		{
			Point = new Vector3(point.x, 0, point.z);
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
			
			var edgeData = AdjustMinorRoadLength();

			var edgeToSnap = ReturnClosestEdgeOnMesh(pbMeshMain, edgeData.Center);
			
			var angleSelected = 0f;
			var faceCenter = Vector3.zero;
			
			if (FindClosestFacesToPoint(pbMeshMain, edgeData.Center, out var orderedFaces))
			{
				var edgeDatas = new List<EdgeData>();
				var firstFace = orderedFaces.FirstOrDefault();
				faceCenter = firstFace.Value;
				
				var wc = WingedEdge.GetWingedEdges(pbMeshMain, new[] {firstFace.Key});
				
				foreach (var we in wc.FindAll(f => f.opposite == null))
				{
					edgeDatas.Add(EdgeData.CalculateForEdge(pbMeshMain, we.edge.local));
				}

				if (edgeDatas.Count > 0)
				{
					var proxy = new EdgeProximitySelector(edgeDatas);
					edgeToSnap = proxy.CalculateProximity(edgeData);
				}
				else
				{
					Debug.Log("Dot problem on :" + pbMeshMinor);
				}
			}
			
			
			//Adjust size in case of low width
			if (edgeToSnap.Length < 1f)
			{
				var min = 1f - edgeToSnap.Length;
				pbMeshMain.TranslateVertices(new [] {edgeToSnap.Edge.a}, edgeToSnap.Dir * min / 2);
				pbMeshMain.TranslateVertices(new [] {edgeToSnap.Edge.b}, -edgeToSnap.Dir * min / 2);
			}
			
			Debug.Log($"Edge to snap [{pbMeshMinor.name},{edgeData.Center}]:{edgeToSnap.Center} | DotSelected:{angleSelected}| Face:{faceCenter}");
		
			SnapVerticles(pbMeshMain, edgeToSnap, pbMeshMinor, edgeData);
			//Combine(pbMeshMain, pbMeshMinor);
		}

		private void Combine(ProBuilderMesh pbMeshMain, ProBuilderMesh pbMeshMinor)
		{
			//Combine to single mesh
			var mesh = CombineMeshes.Combine(new[] {pbMeshMain, pbMeshMinor}, pbMeshMain);
			var first = mesh[0];
			first.WeldVertices(mesh[0].faces.SelectMany(s => s.indexes), 0.2f);
			first.ToMesh(MeshTopology.Quads);
			first.Refresh();

			GameObject.DestroyImmediate(MinorRoad.Builder.GameObject);
		}

		private void SnapVerticles(ProBuilderMesh pbMeshMain, EdgeData edgeToSnap, ProBuilderMesh pbMeshMinor, EdgeData edgeData)
		{
			var mainVertA = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.b];
			var mainVertB = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.a];
			var vert = pbMeshMinor.GetVertices();
			vert[edgeData.Edge.a].position = mainVertA;
			vert[edgeData.Edge.b].position = mainVertB;
			pbMeshMinor.SetVertices(vert);
			pbMeshMinor.ToMesh();
			pbMeshMinor.Refresh();
		}

		float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n){
			// angle in [0,180]
			float angle = Vector3.Angle(a,b);
			float sign = Mathf.Sign(Vector3.Dot(n,Vector3.Cross(a,b)));

			// angle in [-179,180]
			float signed_angle = angle * sign;

			// angle in [0,360] (not used but included here for completeness)
			//float angle360 =  (signed_angle + 180) % 360;

			return signed_angle;
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
			
			//Can't create new bevel, find closest face and return edges
			if (newFace == null)
			{
				if (FindClosestFacesToPoint(pbMesh, Point, out var faces))
				{
					wingedEdges = WingedEdge.GetWingedEdges(pbMesh, new[] {faces.FirstOrDefault().Key});
				}
				else
					Debug.LogError(edgeData.Edge + $" Center: {edgeData.Center} Minor: {MinorRoad.Name}");
				
				return wingedEdges;
			}
			
			Debug.Log("Bevel done: "+ edgeData.Edge);
			wingedEdges = WingedEdge.GetWingedEdges(pbMesh, newFace);
			pbMesh.ToMesh();
			pbMesh.Refresh();

			return wingedEdges;
		}

		private int RemoveToTightFaces(ProBuilderMesh pbMesh, Vector2 startPoint, float distance = 0.5f)
		{
			if (!FindClosestFacesToPoint(pbMesh, Point, out var facesToCheck))
				return -1;
			
			var count = 0;
			var prevFace = facesToCheck[0].Key;
			foreach (var pair in facesToCheck)
			{
				var nextFace = pair.Key;
				if (nextFace == prevFace) continue;
				//Use 2d vector, Y pos is different
				var v2d = new Vector2(pair.Value.x, pair.Value.z);
				if (Vector2.Distance(v2d, startPoint) < distance)
				{
					pbMesh.DeleteFace(prevFace);
					pbMesh.ToMesh();
					pbMesh.Refresh();
					count++;
					Debug.LogWarning($"Removed face:{pair.Key}");
				}

				//set next point
				startPoint = v2d;
				prevFace = nextFace;
			}
			
			pbMesh.WeldVertices(pbMesh.faces.SelectMany(s => s.indexes), distance);
			pbMesh.ToMesh(MeshTopology.Quads);
			pbMesh.Refresh();
			return count;
		}
		private EdgeData AdjustMinorRoadLength()
		{
			var pbMesh = MinorRoad.Builder.ProBuilderMesh;
			var edgeData = GetClosesEdge(MinorRoad, Point);
			
			if (FindClosestFacesToPoint(pbMesh, Point,out var faceData))
			{
				pbMesh.TranslateVertices(new[] {faceData[0].Key}, MinorRoadDir * (edgeData.Length / 2));
				
				//pbMesh.TranslateVertices(new[] {w.Last()}, MinorRoadDir * -(edgeData.Length / 2));
				pbMesh.ToMesh(MeshTopology.Quads);
				pbMesh.Refresh();
			}
			
			MinorRoad.Invalidate();
			if (MinorRoad.Name.Equals("545317729"))
			{
				Debug.Log($"Problem: {Point} {edgeData.Center}");
			}
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

		private bool FindClosestFacesToPoint(ProBuilderMesh mesh, Vector3 point, out List<KeyValuePair<Face, Vector3>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Face, Vector3>>();
			var orderedList = new Dictionary<Face, Vector3>();
			//Don't use on to small objects
			if (mesh.faceCount < 2)
				return false;	
			
			foreach (var face in mesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(mesh.positions, new[] {edge.a, edge.b})).ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos);
			}

			if (orderedList.Count == 0)
				return false;
			
			orderedFaces.AddRange(orderedList.OrderBy(o => Vector3.Distance(o.Value, point)));
			return true;
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