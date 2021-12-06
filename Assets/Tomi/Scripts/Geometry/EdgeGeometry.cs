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
	public class EdgeGeometry: Geometry
	{
		public List<EdgeData> Edges { get; }

		internal enum EdgeSearchType
		{
			All,
			OnlyExternal,
			OnlyInternal
		}
		
		private readonly Dictionary<Vector2, UnityEngine.ProBuilder.Face> _beveledPoints;
		
		public EdgeGeometry(ProBuilderMesh mesh, List<Vector3> points):base(mesh, points)
		{
			Edges = new List<EdgeData>();
			_beveledPoints = new Dictionary<Vector2, UnityEngine.ProBuilder.Face>();
			CalculateRoadEdgeData();
		}

		internal bool GetClosesEdge(Vector2 p, out EdgeData edge, EdgeSearchType edgeSearchType = EdgeSearchType.All, bool ignoreAtPoint = false)
		{
			var all = new List<EdgeData>();

			switch (edgeSearchType)
			{
				case EdgeSearchType.OnlyInternal:
						all.AddRange(Edges.FindAll(f=> f.Internal == true));
					break;
				
				case EdgeSearchType.OnlyExternal:
						all.AddRange(Edges.FindAll(f=>f.Internal == false));
					break;
				
				case EdgeSearchType.All:
						all.AddRange(Edges);
					break;
			}
			
			var distance = Mathf.Infinity;
			var hit = false;
			edge = new EdgeData();
			
			for (int i = 0; i < all.Count; i++)
			{
				var dst = Vector2.Distance(p, all[i].Center);
				if (ignoreAtPoint && dst < Mathf.Epsilon) continue;
				if (dst < distance)
				{
					distance = dst;
					edge = all[i];
					hit = true;
				}
			}
			return hit;
		}

		private KeyValuePair<UnityEngine.ProBuilder.Edge, Vector2> FindClosestEdgeToPoint(Vector2 point)
		{
			var orderedList = new Dictionary<UnityEngine.ProBuilder.Edge, float>();

			foreach (var face in PbMesh.faces)
			{
				foreach (var edge in face.edges)
				{
					var edgeCenter = Math.Average(PbMesh.positions, new[] { edge.a, edge.b }).ToVector2();
					var dist = Vector2.Distance(point, edgeCenter);
					orderedList.Add(edge, dist);
				}
			}

			if (orderedList.Count > 0)
			{
				var orderedEnumerable = orderedList.OrderBy(o => o.Value).ToArray();
				var first = orderedEnumerable[0];

				var pos = Math.Average(PbMesh.positions, new[] { first.Key.a, first.Key.b });
				return new KeyValuePair<UnityEngine.ProBuilder.Edge, Vector2>(first.Key,pos.ToVector2());
			}

			throw new Exception($"No faces in mesh {PbMesh}");
		}

		
		public UnityEngine.ProBuilder.Face BevelAtPoint(Vector2 point)
		{
			if (_beveledPoints.ContainsKey(point))
			{
				Debug.Log($"Point [{point}] already beveled");
				return _beveledPoints[point];
			}

			if (!GetClosesEdge(point, out var edgeData, EdgeSearchType.OnlyInternal))
			{
				Debug.LogError($"Cant find proper edge to bevel:{PbMesh.name}, {point}");
			}

			var newFace = Bevel.BevelEdges(PbMesh, new[] {edgeData.Edge}, edgeData.Length / 2);

			//Can't create new bevel
			if (newFace == null)
			{
				Debug.LogError($"Cant bevel here:{PbMesh} - {point}");
				return null;
			}

			//Remove old edge, as it is overlapped by beveled face
			Edges.Remove(edgeData);
			
			var face = newFace[0];
			
			foreach (var a in face.edges)
			{
				var data = new EdgeData(PbMesh, a);
				var dot = Vector2.Dot(data.Dir, edgeData.Dir);
				if (Mathf.Abs(dot) > 0.9f)
				{
					data.Internal = true;
				}
				
				Edges.Add(data);
			}
			_beveledPoints.Add(point,face);
			return face;
		}
		
		private void CalculateRoadEdgeData()
		{
			var edgeData = new List<EdgeData>();
			
			for (var index = 0; index < PbMesh.faces.Count; index++)
			{
				for (var i = 0; i < PbMesh.faces[index].edges.Count; i++)
				{
					var faceEdge = PbMesh.faces[index].edges[i];
					var edge = new EdgeData(PbMesh, faceEdge);
					edge.CheckIsInternal(Points);
					edgeData.Add(edge);
				}
			}

			var internalPoints = edgeData.Count(c => c.Internal);
			Debug.Assert(internalPoints == Points.Count);
			Edges.AddRange(edgeData);
		}
		
		private Vector2 CalculateDirectionFromEdgeIndexZero()
		{
			var first = Edges.Find(f => f.InternalIndex == 0 && f.Internal);
			if (!GetClosesEdge(first.Center, out var second, EdgeSearchType.OnlyInternal))
				throw new Exception("Cant set second edge");
			return (first.Center - second.Center).normalized;
		}

		internal Vector2 CalculateDirectionFromEdge(EdgeData edgeData)
		{
			if (!GetClosesEdge(edgeData.Center, out var firstEdge, EdgeSearchType.OnlyInternal))
				throw new Exception("Cant set first edge");
			
			if (!GetClosesEdge(firstEdge.Center, out var secondEdge, EdgeSearchType.OnlyInternal, true))
				throw new Exception("Cant set second edge");
			
			if (firstEdge.InternalIndex == -1 || secondEdge.InternalIndex == -1)
				throw new Exception("Indexes must be set to calculate direction");
			
			return firstEdge.InternalIndex > secondEdge.InternalIndex ?
				(firstEdge.Center - secondEdge.Center).normalized :
				(secondEdge.Center - firstEdge.Center).normalized;
		}
		
		public EdgeData ReturnClosestEdgeOnMesh(Vector2 point)
		{
			if(!GetClosesEdge(point, out var edgeData, EdgeSearchType.OnlyExternal))
				Debug.LogError("Can't return closest edge");
			
			return edgeData;
			
			var closestEdgeInMainRoad = FindClosestEdgeToPoint(point);
			Debug.Log($"Closes in main: {closestEdgeInMainRoad.Key}");
			return EdgeData.CalculateForEdge(PbMesh, closestEdgeInMainRoad.Key);
		}
		
		//TODO: Refactor to separate FacesService ?
		public List<EdgeData> GetFaceWingedEdges(UnityEngine.ProBuilder.Face face)
		{
			var wc = WingedEdge.GetWingedEdges(PbMesh, new[] {face});
			var edgeDatas = new List<EdgeData>();
			foreach (var we in wc.FindAll(f => f.opposite == null))
			{
				edgeDatas.Add(EdgeData.CalculateForEdge(PbMesh, we.edge.local));
			}

			return edgeDatas;
		}

		public void ResizeEdge(EdgeData edgeData)
		{
			//Adjust size in case of low width
			if (edgeData.Length >= 0.9f)
				return;
			
			var min = 1f - edgeData.Length;
			PbMesh.TranslateVertices(new [] {edgeData.Edge.a}, edgeData.Dir * min / 2);
			PbMesh.TranslateVertices(new [] {edgeData.Edge.b}, -edgeData.Dir * min / 2);
		}
	}
}