using System;
using System.Collections.Generic;
using System.Linq;
using Mapzen.Unity;
using Mapzen.VectorData;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Tomi.Geometry
{
	public class SplineMeshBuilder
	{
		private readonly SplineHandler _splineHandler;

		private const string UvGridMaterialGUID = "7f88baed2a0f94bb3a42f7bc45b5fcf8";
		private const string AsphaltGUID = "4c0619faa9f87524989ff9fa25868a75";
		public GameObject MeshObject { get; private set; }
		public Mesh Mesh { get; private set; }
		public ProBuilderMesh PbMesh { get; private set; }
		public EdgeGeometry EdgeGeometry { get; private set; }
		public FaceGeometry FaceGeometry { get; private set; }
		public bool AlteredBefore => _previousModifications.Count > 0;
		
		private ProBuilderMesh _proMesh;
		private MeshFilter _meshFilter;
		
		private readonly Dictionary<EdgeData, EdgeData> _previousModifications;
		public SplineMeshBuilder(SplineHandler splineHandler)
		{
			_splineHandler = splineHandler;
			_previousModifications = new Dictionary<EdgeData, EdgeData>();
		}
		
		private Material LoadRoadMaterial()
		{
			Material material = new Material(Shader.Find("Default"));
#if UNITY_EDITOR
			var path = AssetDatabase.GUIDToAssetPath(AsphaltGUID);
			material = AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
			return material;
		}

		public void UpdateMesh(Mesh mesh)
		{
			if (_meshFilter == null)
				throw new Exception("Mesh is not builded yet");
			Mesh = mesh;
			_meshFilter.mesh = Mesh;
		}
		
		public void Build(Transform parent)
		{
			var pointsData = _splineHandler.Points;
			var meshData = new MeshData();

			var v2List = new List<Vector2>();
			foreach (var vector3 in pointsData)
			{
				v2List.Add(new Vector2(vector3.x,vector3.z));
			}
			
			var builder = new PolylineBuilder(meshData, _splineHandler.PolylineOptions, Matrix4x4.identity, _splineHandler.Name);
			builder.OnBeginLineString();

			for (var index = 0; index < pointsData.Count; index++)
			{
				builder.OnPoint(new Point(pointsData[index].x, pointsData[index].z));
			}
	
			builder.OnEndLineString();

			MeshObject = new GameObject(_splineHandler.Name)
			{
				transform =
				{
					parent = parent.transform
				}
			};

			Mesh = new Mesh();
			GenerateMesh(MeshObject, meshData);
		}

		private void GenerateMesh(GameObject gameObject, MeshData meshData)
		{
			var meshBucket = meshData.Meshes[0];
			
			Mesh.SetVertices(meshBucket.Vertices);
			Mesh.SetUVs(0, meshBucket.UVs);
			Mesh.subMeshCount = meshBucket.Submeshes.Count;
			for (int s = 0; s < meshBucket.Submeshes.Count; s++)
			{
				Mesh.SetTriangles(meshBucket.Submeshes[s].Indices, s);
			}

			Mesh.RecalculateNormals();
			
			var materials = meshBucket.Submeshes.Select(s => s.Material).ToArray();
			
			_meshFilter = gameObject.AddComponent<MeshFilter>();
			var meshRendererComponent = gameObject.AddComponent<MeshRenderer>();
			meshRendererComponent.materials = materials;
			_meshFilter.mesh = Mesh;
			var importer = new MeshImporter(MeshObject);
			importer.Import();
			_meshFilter.sharedMesh = new Mesh();
			_proMesh = MeshObject.GetComponent<ProBuilderMesh>();
			PbMesh = _proMesh;
			//Generate edge data for mesh
			InitGeometry();
			UpdatePbMesh();
		}

		public bool WasModified(EdgeData toEdge)
		{
			var modifiedBefore = _previousModifications.ContainsKey(toEdge);
			return modifiedBefore;
		}

		private List<EdgeData> GetEdgesFromFace(Face face)
		{
			var edges = PbMesh.GetPerimeterEdges(new[] {face});
			var edgesData = new List<EdgeData>();
			foreach (var edge in edges)
			{
				var e = EdgeGeometry.Edges.Find(f => f.Edge == edge);
				edgesData.Add(e);
			}
			return edgesData;
		}
		public EdgeData GetBestEdgeFromFace(Face mainFace, EdgeData minorEdge)
		{
			var proxy = new EdgeProximitySelector(GetEdgesFromFace(mainFace));
			var point= proxy.CalculateProximity(minorEdge);
			if (PbMesh.name.Equals("328247473"))
			{
				return point;
			}
			else
			{
				return point;
			}
		}
		
		public EdgeData AdjustEndPosition(EdgeData toEdge, float roadSize = 1f)
		{
			if (EdgeGeometry.Edges.Count == 0)
				throw new Exception("No edges calculated yet");

			if (!EdgeGeometry.GetClosesEdge(toEdge.Center, out var edgeData, EdgeGeometry.Type.Cap))
				if(!EdgeGeometry.GetClosesEdge(toEdge.Center, out edgeData, EdgeGeometry.Type.External))
					throw new Exception("Cant find proper edge to snap");
			
			Vector2 dir;
			try
			{
				dir = EdgeGeometry.CalculateDirectionFromEdge(edgeData);
			}
			catch (Exception e)
			{
				Debug.LogError($"Cant calculate for {PbMesh.name}");
				return new EdgeData();
			}

			var length = Vector2.Distance(toEdge.Center, edgeData.Center);
			
			TranslateEdge(edgeData,dir * length);
			UpdatePbMesh();
			return EdgeGeometry.Edges.Find(f => f.Edge == edgeData.Edge);
		}

		public void TranslateEdge(EdgeData edgeData, Vector2 offset)
		{
			var offsetV3 = new Vector3(offset.x, 0, offset.y);
			PbMesh.TranslateVertices(new [] {edgeData.Edge}, offsetV3);
		}

		private void InitGeometry()
		{
			EdgeGeometry = new EdgeGeometry(_proMesh, _splineHandler.Points);
			FaceGeometry = new FaceGeometry(_proMesh, _splineHandler.Points);
		}
		public void UpdatePbMesh()
		{
			_proMesh.ToMesh(MeshTopology.Quads);
			_proMesh.Refresh();
			EdgeGeometry.Refresh();
			FaceGeometry.Refresh();
			//DrawDebug();
		}

		private List<GameObject> _gameObjects;

		private void DrawDebug()
		{
			if (_gameObjects != null)
			{
				foreach (var o in _gameObjects)
				{
					GameObject.DestroyImmediate(o);
				}
			}
			else
			{
				_gameObjects = new List<GameObject>();
			}
			
			foreach (var edgeData in EdgeGeometry.Edges)
			{
				var e = GameObject.CreatePrimitive(edgeData.Internal ? PrimitiveType.Cylinder: PrimitiveType.Cube);
				e.transform.SetParent(PbMesh.transform);
				e.transform.localScale = Vector3.one / 4;
				e.transform.position = new Vector3(edgeData.Center.x, 1, edgeData.Center.y);
				e.name = $"E-{edgeData.Edge.a}-{edgeData.Edge.b}";
				_gameObjects.Add(e);
			}
		}
	}
}