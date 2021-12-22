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
		public bool FindClosestFacesToPoint(Vector2 point, out List<KeyValuePair<Vector2,Face>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Vector2,Face>>();
			var orderedList = new Dictionary<Vector2,Face>();
			//Don't use on to small objects
			if (PbMesh.faceCount < 2)
				return false;

			foreach (var face in PbMesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(PbMesh.positions, new[] {edge.a, edge.b}).ToVector2())
					.ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(pos,face);
			}
			
			if (orderedList.Count == 0)
				return false;

			orderedFaces.AddRange(orderedList.OrderBy(o => Vector2.Distance(o.Key, point)));
			return true;
		}

		public bool ClosestFace(Vector2 point, out KeyValuePair<Vector2,Face> face)
		{
			face = new KeyValuePair<Vector2,Face>();
			if (!FindClosestFacesToPoint(point, out var facesToCheck))
				return false;
			
			face = facesToCheck[0];
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
			
			var dir = (f1.Key - f0.Key).normalized;
			var dirV3 = new Vector3(dir.x, 0, dir.y);
			var edges = new List<WingedEdge>();
			
			//don't move back big spacy faces
			if (Vector2.Distance(f0.Key, f1.Key) > 2f)
			{
				PbMesh.TranslateVertices(new[] {f0.Value}, dirV3 * .5f);
			}
			else
			{
				PbMesh.TranslateVertices(new[] {f0.Value, f1.Value}, dirV3 * .5f);
				edges = WingedEdge.GetWingedEdges(PbMesh, new []{f1.Value});
			}
			//move back internal edges 
			foreach (var internalEdge in edges.FindAll(f=>f.opposite != null))
			{
				PbMesh.TranslateVertices(new []{internalEdge.edge.local}, -dirV3*0.5f);
			}
			
			return true;
		}
		public int RemoveToTightFaces(Vector2 startPoint, float distance = 0.5f)
		{
			if (!FindClosestFacesToPoint(startPoint, out var facesToCheck))
				return -1;

			var count = 0;
			var prevFace = facesToCheck[0].Value;
			foreach (var pair in facesToCheck)
			{
				var nextFace = pair.Value;
				if (nextFace == prevFace) continue;
			
				if (Vector2.Distance(pair.Key, startPoint) < distance)
				{
					MergeFacesAt(pair.Key);
					PbMesh.ToMesh();
					PbMesh.Refresh();
					count++;
					Debug.LogWarning($"Removed face:{pair.Key}");
				}

				//set next point
				startPoint = pair.Key;
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
			return MergeElements.Merge(PbMesh, new[] {first.Value, second.Value});
		}
	}
}