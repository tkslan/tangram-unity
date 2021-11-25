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
		public GameObject GameObject { get; private set; }
		public Mesh Mesh { get; private set; }
		public ProBuilderMesh PbMesh { get; private set; }

		public EdgeService EdgeService { get; private set; }

		private ProBuilderMesh _proMesh;
		private MeshFilter _meshFilter;
		public SplineMeshBuilder(SplineHandler splineHandler)
		{
			_splineHandler = splineHandler;
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

			GameObject = new GameObject(_splineHandler.Name)
			{
				transform =
				{
					parent = parent.transform
				}
			};

			Mesh = new Mesh();
			GenerateMesh(GameObject, meshData);
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
			var importer = new MeshImporter(GameObject);
			importer.Import();
			_meshFilter.sharedMesh = new Mesh();
			_proMesh = GameObject.GetComponent<ProBuilderMesh>();
			PbMesh = _proMesh;
			//Generate edge data for mesh
			EdgeService = new EdgeService(_proMesh, _splineHandler.Points);
		}

		public void UpdatePbMesh()
		{
			_proMesh.ToMesh(MeshTopology.Quads);
			_proMesh.Refresh();
		}
	}
}