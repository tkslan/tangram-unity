using System;
using System.Collections.Generic;
using System.Linq;
using Tomi.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Math = UnityEngine.ProBuilder.Math;

namespace Tomi.Geometry
{
	public class EdgeService
	{
		public List<EdgeData> Edges { get; }
		
		private enum EdgeSearchType
		{
			All,
			OnlyExternal,
			OnlyInternal
		}
		
		private readonly ProBuilderMesh _pbMesh;
		private readonly List<Vector3> _points;
		
		public EdgeService(ProBuilderMesh mesh, List<Vector3> points)
		{
			_pbMesh = mesh;
			_points = points;
			Edges = new List<EdgeData>();
			CalculateRoadEdgeData();
		}
		
		private EdgeData GetClosesEdge(Vector2 p, EdgeSearchType edgeSearchType = EdgeSearchType.All)
		{
			var distance = Mathf.Infinity;
			var best = new EdgeData();
			
			for (int i = 0; i < Edges.Count; i++)
			{
				//Ignore internal edges
				if (edgeSearchType.Equals(EdgeSearchType.OnlyExternal) && Edges[i].Internal)
					continue;
				//Ignore external edges
				if (edgeSearchType.Equals(EdgeSearchType.OnlyInternal) && !Edges[i].Internal)
					continue;
				
				var dst = Vector2.Distance(p, Edges[i].Center);
				
				if (distance < dst)
				{
					distance = dst;
					best = Edges[i];
				}
			}

			return best;
			/*
			var closes = FindClosestEdgeToPoint(p);
			return EdgeData.CalculateForEdge(_pbMesh,closes.Key);
			*/
		}

		private KeyValuePair<Edge, Vector2> FindClosestEdgeToPoint(Vector2 point)
		{
			var orderedList = new Dictionary<Edge, float>();

			foreach (var face in _pbMesh.faces)
			{
				foreach (var edge in face.edges)
				{
					var edgeCenter = Math.Average(_pbMesh.positions, new[] { edge.a, edge.b }).ToVector2();
					var dist = Vector2.Distance(point, edgeCenter);
					orderedList.Add(edge, dist);
				}
			}

			if (orderedList.Count > 0)
			{
				var orderedEnumerable = orderedList.OrderBy(o => o.Value).ToArray();
				var first = orderedEnumerable[0];

				var pos = Math.Average(_pbMesh.positions, new[] { first.Key.a, first.Key.b });
				return new KeyValuePair<Edge, Vector2>(first.Key,pos.ToVector2());
			}

			throw new Exception($"No faces in mesh {_pbMesh}");
		}

		public Face BevelAtPoint(Vector2 point)
		{
			var edgeData = GetClosesEdge(point);
			var newFace = Bevel.BevelEdges(_pbMesh, new[] {edgeData.Edge}, edgeData.Length / 2);

			//Can't create new bevel, find closest face and return edges
			if (newFace == null)
				throw new Exception("Cant create bevel here");

			var face = newFace[0];
			foreach (var a in face.edges)
			{
				var data = new EdgeData(_pbMesh, a);
				if (Vector2.Dot(data.Dir, edgeData.Dir) > 0.9f)
				{
					data.Internal = true;
				}
				
				Edges.Add(data);
			}

			return face;
		}
		
		private void CalculateRoadEdgeData()
		{
			var edgeData = new List<EdgeData>();
			
			for (var index = 0; index < _pbMesh.faces.Count; index++)
			{
				for (var i = 0; i < _pbMesh.faces[index].edges.Count; i++)
				{
					var faceEdge = _pbMesh.faces[index].edges[i];
					var edge = new EdgeData(_pbMesh, faceEdge);
					edge.CheckIsInternal(_points);
					edgeData.Add(edge);
				}
			}
			Debug.Assert(edgeData.Count(c=>c.Internal) == _points.Count);
			Edges.AddRange(edgeData);
		}

		public EdgeData AdjustEndPosition(Vector2 point)
		{
			if (Edges.Count == 0)
				throw new Exception("");
			
			var edgeData = GetClosesEdge(point);
			
			if (FindClosestFacesToPoint( point,out var faceData))
			{
				//var roadDirection = (faceData[0].Value - faceData[1].Value).normalized;
				var roadDirection = CalculateDirectionFromEdge(edgeData);
				
				_pbMesh.TranslateVertices(new[] {faceData[0].Key}, roadDirection * (edgeData.Length / 2));
				
				//pbMesh.TranslateVertices(new[] {w.Last()}, MinorRoadDir * -(edgeData.Length / 2));
				_pbMesh.ToMesh(MeshTopology.Quads);
				_pbMesh.Refresh();
			}

			return edgeData;
		}

		private Vector2 CalculateDirectionFromEdge(EdgeData edgeData)
		{
			var first = GetClosesEdge(edgeData.Center, EdgeSearchType.OnlyInternal);
		
			var second = GetClosesEdge(first.Center, EdgeSearchType.OnlyInternal);
			
			if (first.InternalIndex == -1 || second.InternalIndex == -1)
				throw new Exception("Indexes must be set to calculate direction");
			
			return first.InternalIndex > second.InternalIndex ?
				(first.Center - second.Center).normalized :
				(second.Center - first.Center).normalized;
		}
		
		public EdgeData ReturnClosestEdgeOnMesh(Vector2 point)
		{
			var closestEdgeInMainRoad = FindClosestEdgeToPoint(point);
			Debug.Log($"Closes in main: {closestEdgeInMainRoad.Key}");
			return EdgeData.CalculateForEdge(_pbMesh, closestEdgeInMainRoad.Key);
		}
		
		//TODO: Refactor to separate FacesService ?
		public List<EdgeData> GetFaceWingedEdges(Face face)
		{
			var wc = WingedEdge.GetWingedEdges(_pbMesh, new[] {face});
			var edgeDatas = new List<EdgeData>();
			foreach (var we in wc.FindAll(f => f.opposite == null))
			{
				edgeDatas.Add(EdgeData.CalculateForEdge(_pbMesh, we.edge.local));
			}

			return edgeDatas;
		}

		public void ResizeEdge(EdgeData edgeData)
		{
			//Adjust size in case of low width
			if (edgeData.Length >= 0.9f)
				return;
			
			var min = 1f - edgeData.Length;
			_pbMesh.TranslateVertices(new [] {edgeData.Edge.a}, edgeData.Dir * min / 2);
			_pbMesh.TranslateVertices(new [] {edgeData.Edge.b}, -edgeData.Dir * min / 2);
		}
		public bool FindClosestFacesToPoint(Vector3 point, out List<KeyValuePair<Face, Vector3>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Face, Vector3>>();
			var orderedList = new Dictionary<Face, Vector3>();
			//Don't use on to small objects
			if (_pbMesh.faceCount < 2)
				return false;	
			
			foreach (var face in _pbMesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(_pbMesh.positions, new[] {edge.a, edge.b})).ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos);
			}

			if (orderedList.Count == 0)
				return false;
			
			orderedFaces.AddRange(orderedList.OrderBy(o => Vector3.Distance(o.Value, point)));
			return true;
		}

		private int RemoveToTightFaces(ProBuilderMesh pbMesh, Vector2 startPoint, float distance = 0.5f)
		{
			if (!FindClosestFacesToPoint( startPoint, out var facesToCheck))
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
	}
}