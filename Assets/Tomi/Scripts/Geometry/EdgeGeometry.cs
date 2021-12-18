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
			Internal,
			Cap
		}
		
		private readonly Dictionary<Vector2, Face> _beveledPoints;
		
		public EdgeGeometry(ProBuilderMesh mesh, List<Vector3> points):base(mesh, points)
		{
			Edges = new List<EdgeData>();
			_beveledPoints = new Dictionary<Vector2, Face>();
			Refresh();
		}
		
		private List<EdgeData> GetEdgesByType(Type type)
		{
			return type switch
			{
				Type.Internal => Edges.FindAll(f => f.Internal),
				Type.External => Edges.FindAll(f => !f.Internal),
				Type.Cap => Edges.FindAll(f => !f.Internal && f.Index >= 0),
				_ => Edges
			};
		}

		public bool MoveBackNextEdges(Vector2 point)
		{
			if (!FindClosestEdges(point, out var keyValuePairs))
				return false;

			if (keyValuePairs.Count < 2)
				return false;
			
			var e0 = keyValuePairs[0];
			var e1 = keyValuePairs[1];
			var dir = (e1.Value - e0.Value).normalized;
			TranslateEdge(e0.Key, dir);
			return true;
		}
		internal bool GetClosesEdge(Vector2 p, out EdgeData edge, Type type = Type.Any, bool ignoreAtPoint = false)
		{
			var all = GetEdgesByType(type);
			
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

		private bool FindClosestEdges(Vector2 point, out List<KeyValuePair<EdgeData, Vector2>> edges)
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

			edges = new List<KeyValuePair<EdgeData, Vector2>>();
			
			if (orderedList.Count == 0)
				return false;
			
			foreach (var edge in orderedList)
			{
				var pos = Math.Average(PbMesh.positions, new[] { edge.Key.a, edge.Key.b }).ToVector2();
				var ed = Edges.Find(f => f.Edge == edge.Key);
				
				if (!ed.Valid)
				{
					Debug.LogError("Cant find edge data");
					continue;
				}

				edges.Add(new KeyValuePair<EdgeData, Vector2>(ed, pos));
			}

			return true;
		}

		public Face BevelAtPoint(Vector2 point, out List<EdgeData> sideEdges)
		{
			sideEdges = new List<EdgeData>();
			if (_beveledPoints.ContainsKey(point))
			{
				Debug.Log($"Point [{point}] already beveled");
				return _beveledPoints[point];
			}

			if (!GetClosesEdge(point, out var edgeData, Type.Internal))
				if(!GetClosesEdge(point, out edgeData, Type.Cap))
				{
					Debug.LogError($"Cant find proper edge to bevel:{PbMesh.name}, {point}");
				}

			//var newFace = UnityEngine.ProBuilder.MeshOperations.ConnectElements.Connect();
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
			
			//Update points array with newly created points
			foreach (var a in face.edges)
			{
				var data = new EdgeData(PbMesh, a);
				var dot = Vector2.Dot(data.Dir, edgeData.Dir);
				if (Mathf.Abs(dot) > 0.9f)
				{
					sideEdges.Add(data);
					Points.Add(data.Center);
				}
			}
			
			_beveledPoints.Add(point, face);
			
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

		internal Vector2 CalculateDirectionFromEdge(EdgeData edgeData)
		{
			//First find a cap edge
			if (!GetClosesEdge(edgeData.Center, out var firstEdge))
				throw new Exception("Cant set first edge");
			//Then find first internal edge from cap and calculate direction from center points
			if (!GetClosesEdge(firstEdge.Center, out var secondEdge, Type.Internal, true))
				if(!GetClosesEdge(firstEdge.Center, out secondEdge, Type.Cap))
					throw new Exception("Cant set second edge");
			
			return firstEdge.Index > secondEdge.Index ?
				(firstEdge.Center - secondEdge.Center).normalized :
				(secondEdge.Center - firstEdge.Center).normalized;
		}
		
		public EdgeData ReturnClosestEdgeOnMesh(Vector2 point)
		{
			if(!GetClosesEdge(point, out var edgeData, Type.External))
				Debug.LogError("Can't return closest edge");
			Debug.Log($"Closes from {point} {edgeData.Center}");
			return edgeData;
		}

		public void ResizeEdge(EdgeData edgeToSnap)
		{
			if (edgeToSnap.Length < 0.5f)
			{
				var dir = new Vector3(edgeToSnap.Dir.x, 0, edgeToSnap.Dir.y);
				PbMesh.TranslateVertices(new []{edgeToSnap.Edge.a}, dir * (0.5f - edgeToSnap.Length /3));
				PbMesh.TranslateVertices(new []{edgeToSnap.Edge.b}, -dir * (0.5f - edgeToSnap.Length /3));
			}
		}
	}
}