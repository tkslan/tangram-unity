using System.Collections.Generic;
using Mapzen;
using Mapzen.Unity;
using Mapzen.VectorData;
using Tomi;
using UnityEngine;

public class SplineTileTask : ITileTask
{
	// The tile address this task is working on
	private TileAddress address;
	// The transform applied to the geometry built the tile task builders
	private Matrix4x4 transform;
	// The generation of this tile task
	private int generation;
	// The resulting data of the tile task is stored in this container
	private List<FeatureMesh> data;
	// The map styling this tile task is working on
	private MapStyle featureStyling;

	public int Generation
	{
		get { return generation; }
	}

	public List<FeatureMesh> Data
	{
		get { return data; }
	}

	public List<SplineHandler> SplineHandlers;
	public SplineTileTask(MapStyle featureStyling, TileAddress address, Matrix4x4 transform, int generation)
	{
		this.data = new List<FeatureMesh>();
		SplineHandlers = new List<SplineHandler>();
		this.address = address;
		this.transform = transform;
		this.generation = generation;
		this.featureStyling = featureStyling;
	}

	/// <summary>
	/// Runs the tile task, resulting data will be stored in Data.
	/// </summary>
	/// <param name="featureCollections">The feature collections this tile task will be building.</param>
	public void Start(IEnumerable<FeatureCollection> featureCollections)
	{
		float inverseTileScale = 1.0f / (float)address.GetSizeMercatorMeters();

		foreach (var styleLayer in featureStyling.Layers)
		{
			foreach (var collection in featureCollections)
			{
				foreach (var feature in styleLayer.GetFilter().Filter(collection))
				{
					var layerStyle = styleLayer.Style;
					string featureName = "";
					object identifier;

					if (feature.TryGetProperty("id", out identifier))
					{
						featureName += identifier.ToString();
					}

					// Resulting data for this feature.
					FeatureMesh featureMesh = new FeatureMesh(address.ToString(), collection.Name, styleLayer.Name, featureName);
					
					if (feature.Type is GeometryType.LineString or GeometryType.MultiLineString)
					{
						var polylineOptions = layerStyle.GetPolylineOptions(feature, inverseTileScale);

						if (polylineOptions.Enabled)
						{
							var pb = new PolylineBuilder(featureMesh.Mesh, polylineOptions, transform, featureName);
							
							pb.OnFinished = list =>
							{
								var spline = new SplineHandler(list, featureName, transform);
								SplineHandlers.Add(spline);
							};

							feature.HandleGeometry(pb);
							data.Add(featureMesh);
						}
					}

				}
			}
		}
	}
}