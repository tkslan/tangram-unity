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

		internal enum Type
		{
			Any,
			External,
			Internal
		}
		
		private readonly Dictionary<Vector2, Face> _beveledPoints;
		
		public EdgeGeometry(ProBuilderMesh mesh, List<Vector3> points):base(mesh, points)
		{
			Edges = new List<EdgeData>();
			_beveledPoints = new Dictionary<Vector2, Face>();
			Refresh();
		}
		
		internal bool GetClosesEdge(Vector2 p, out EdgeData edge, Type type = Type.Any, bool ignoreAtPoint = false)
		{
			var all = new List<EdgeData>();

			switch (type)
			{
				case Type.Internal:
						all.AddRange(Edges.FindAll(f=> f.Internal));
					break;
				
				case Type.External:
						all.AddRange(Edges.FindAll(f=> !f.Internal));
					break;
				
				case Type.Any:
						all.AddRange(Edges);
					break;
			}
			
			var distance = Mathf.Infinity;
			edge = new EdgeData();
			var hit = false;
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

			return hit && edge.Valid;
		}

		private KeyValuePair<Edge, Vector2> FindClosestEdgeToPoint(Vector2 point)
		{
			var orderedList = new Dictionary<Edge, float>();

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
				return new KeyValuePair<Edge, Vector2>(first.Key,pos.ToVector2());
			}

			throw new Exception($"No faces in mesh {PbMesh}");
		}
		
		public Face BevelAtPoint(Vector2 point)
		{
			if (_beveledPoints.ContainsKey(point))
			{
				Debug.Log($"Point [{point}] already beveled");
				return _beveledPoints[point];
			}

			if (!GetClosesEdge(point, out var edgeData, Type.Internal))
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
					//data.Internal = true;
				}
				
				Edges.Add(data);
			}
			_beveledPoints.Add(point,face);
			
			return face;
		}
		public override void Refresh()
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
			
			Edges.Clear();
			Edges.AddRange(edgeData);
		}
		
		private Vector2 CalculateDirectionFromEdgeIndexZero()
		{
			var first = Edges.Find(f => f.Index == 0);
			if (!GetClosesEdge(first.Center, out var second, Type.Internal, true))
				throw new Exception("Cant set second edge");
			return (first.Center - second.Center).normalized;
		}

		internal Vector2 CalculateDirectionFromEdge(EdgeData edgeData)
		{
			//First find a cap edge
			if (!GetClosesEdge(edgeData.Center, out var firstEdge))
				throw new Exception("Cant set first edge");
			//Then find first internal edge from cap and calculate direction from center points
			if (!GetClosesEdge(firstEdge.Center, out var secondEdge, Type.Internal, true))
				throw new Exception("Cant set second edge");
			
			return firstEdge.Index < secondEdge.Index ?
				(firstEdge.Center - secondEdge.Center).normalized :
				(secondEdge.Center - firstEdge.Center).normalized;
		}
		
		public EdgeData ReturnClosestEdgeOnMesh(Vector2 point)
		{
			
			if(!GetClosesEdge(point, out var edgeData, Type.External))
				Debug.LogError("Can't return closest edge");
			Debug.Log($"Closes from {point} {edgeData.Center}");
			return edgeData;
			
			var closestEdgeToPoint = FindClosestEdgeToPoint(point);
		
			var edge = EdgeData.CalculateForEdge(PbMesh, closestEdgeToPoint.Key);
			Debug.Log($"Closes: {edge.Center}");
			return edge;
		}
		
		//TODO: Refactor to separate FacesService ?
		public List<EdgeData> GetFaceWingedEdges(UnityEngine.ProBuilder.Face face)
		{
			var wc = WingedEdge.GetWingedEdges(PbMesh, new[] {face});
			var edgeDatas = new List<EdgeData>();
			foreach (var we in wc.FindAll(f => f.opposite == null))
			{
				edgeDatas.Add(new EdgeData(PbMesh, we.edge.local));
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