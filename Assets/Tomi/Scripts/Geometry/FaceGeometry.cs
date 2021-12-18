using System.Collections.Generic;
using System.Linq;
using Tomi.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Tomi.Geometry
{
	public class FaceGeometry : Geometry
	{
		public FaceGeometry(ProBuilderMesh mesh, List<Vector3> points) : base(mesh, points) { }
		public override void Refresh() { }
		public bool FindClosestFacesToPoint(Vector2 point, out List<KeyValuePair<Face, Vector2>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Face, Vector2>>();
			var orderedList = new Dictionary<Face, Vector2>();
			//Don't use on to small objects
			if (PbMesh.faceCount < 2)
				return false;

			foreach (var face in PbMesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(PbMesh.positions, new[] {edge.a, edge.b}).ToVector2())
					.ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos);
			}

			if (orderedList.Count == 0)
				return false;

			orderedFaces.AddRange(orderedList.OrderBy(o => Vector2.Distance(o.Value, point)));
			return true;
		}

		public bool MoveBackClosestFace(Vector2 startPoint)
		{
			if (!FindClosestFacesToPoint(startPoint, out var facesToCheck))
				return false;

			if (facesToCheck.Count < 2)
				return false;

			var f1 = facesToCheck[1];
			var f0 = facesToCheck[0];
			var dir = (f1.Value - f0.Value).normalized;
			var dirV3 = new Vector3(dir.x, 0, dir.y);
			
			PbMesh.TranslateVertices(new []{f0.Key}, dirV3 / 2);
			PbMesh.TranslateVertices(new [] {f1.Key},dirV3 / 4);
			//move back internal edges 
			var edges = WingedEdge.GetWingedEdges(PbMesh, new []{f1.Key, f0.Key});
			foreach (var internalEdge in edges.FindAll(f=>f.opposite != null))
			{
				PbMesh.TranslateVertices(new []{internalEdge.edge.local}, -dirV3 / 3);
			}
			return true;
		}
		public int RemoveToTightFaces(Vector2 startPoint, float distance = 0.5f)
		{
			if (!FindClosestFacesToPoint(startPoint, out var facesToCheck))
				return -1;

			var count = 0;
			var prevFace = facesToCheck[0].Key;
			foreach (var pair in facesToCheck)
			{
				var nextFace = pair.Key;
				if (nextFace == prevFace) continue;
			
				if (Vector2.Distance(pair.Value, startPoint) < distance)
				{
					PbMesh.DeleteFace(prevFace);
					PbMesh.ToMesh();
					PbMesh.Refresh();
					count++;
					Debug.LogWarning($"Removed face:{pair.Key}");
				}

				//set next point
				startPoint = pair.Value;
				prevFace = nextFace;
			}

			PbMesh.WeldVertices(PbMesh.faces.SelectMany(s => s.indexes), distance);
			return count;
		}
		
		public Face MergeFacesAt(Vector2 point)
		{
			if (!FindClosestFacesToPoint(point, out var faces))
				return null;
			
			var first = faces[0];
			var second = faces[1];
			return MergeElements.Merge(PbMesh, new[] {first.Key, second.Key});
		}
	}
}