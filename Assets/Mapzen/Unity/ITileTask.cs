using System.Collections.Generic;
using Mapzen.VectorData;

namespace Mapzen.Unity
{
	public interface ITileTask
	{
		int Generation { get; }
		List<FeatureMesh> Data { get; }

		/// <summary>
		/// Runs the tile task, resulting data will be stored in Data.
		/// </summary>
		/// <param name="featureCollections">The feature collections this tile task will be building.</param>
		void Start(IEnumerable<FeatureCollection> featureCollections);
	}
}