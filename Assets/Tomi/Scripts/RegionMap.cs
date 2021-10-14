using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mapzen.VectorData;
using Mapzen.Unity;
using Mapzen.VectorData.Formats;
using Tomi;

namespace Mapzen
{
    public class RegionMap : MonoBehaviour
    {
        // Version information
        // This allows us to check whether an asset was serialized with a different version than this code.
        // If a serialized field of this class is changed or renamed, currentAssetVersion should be incremented.

        private const int currentAssetVersion = 1;
        [SerializeField] private int serializedAssetVersion = currentAssetVersion;
        

        // Public fields
        // These are serialized, so renaming them will break asset compatibility.

        public string ApiKey = "";

        public TileArea Area = new TileArea(
            new LngLat(-74.014892578125, 40.70562793820589),
            new LngLat(-74.00390625, 40.713955826286046),
            16);

        public float UnitsPerMeter = 1.0f;

        public string RegionName = "";

        public SceneGroupType GroupOptions;

        public GameObjectOptions GameObjectOptions;

        public MapStyle Style;

        public bool useTomiMerge;
        // Private fields

        private IO tileIO = new IO();

        private List<ITileTask> tasks = new List<ITileTask>();

        private int nTasksForArea = 0;

        private int generation = 0;

        private AsyncWorker worker = new AsyncWorker(2);

        private GameObject regionMap;

        private TileCache tileCache = new TileCache(50);

        public void DownloadTilesAsync()
        {
            TileBounds bounds = new TileBounds(Area);

            // Abort currently running tasks and increase generation
            worker.ClearTasks();
            tasks.Clear();
            nTasksForArea = 0;
            generation++;

            foreach (var tileAddress in bounds.TileAddressRange)
            {
                nTasksForArea++;
            }

            foreach (var tileAddress in bounds.TileAddressRange)
            {
                float offsetX = (tileAddress.x - bounds.min.x);
                float offsetY = (-tileAddress.y + bounds.min.y);

                float scaleRatio = (float)tileAddress.GetSizeMercatorMeters() * UnitsPerMeter;
                Matrix4x4 scale = Matrix4x4.Scale(new Vector3(scaleRatio, scaleRatio, scaleRatio));
                Matrix4x4 translate = Matrix4x4.Translate(new Vector3(offsetX * scaleRatio, 0.0f, offsetY * scaleRatio));
                Matrix4x4 transform = translate * scale;

                IEnumerable<FeatureCollection> featureCollections = tileCache.Get(tileAddress);

                if (featureCollections != null)
                {
                    var task = new SplineTileTask(Style, tileAddress, transform, generation);

                    worker.RunAsync(() =>
                    {
                        if (generation == task.Generation)
                        {
                            task.Start(featureCollections);
                            tasks.Add(task);
                        }
                    });
                }
                else
                {
                    // Use a local generation variable to be used in IORequestCallback coroutine
                    int requestGeneration = generation;

                    var wrappedTileAddress = tileAddress.Wrapped();

                    var uri = new Uri(string.Format("https://tile.nextzen.org/tilezen/vector/v1/all/{0}/{1}/{2}.mvt?api_key={3}",
                        wrappedTileAddress.z,
                        wrappedTileAddress.x,
                        wrappedTileAddress.y,
                        ApiKey));

                    IO.IORequestCallback onTileFetched = (response) =>
                    {
                        if (requestGeneration != generation)
                        {
                            // Another request has been made before the coroutine was triggered
                            return;
                        }

                        if (response.hasError())
                        {
                            Debug.Log("TileIO Error: " + response.error);
                            return;
                        }

                        if (response.data.Length == 0)
                        {
                            Debug.Log("Empty Response");
                            return;
                        }

                        ITileTask task =  useTomiMerge ? new SplineTileTask(Style, tileAddress, transform, generation):
                                                         new TileTask(Style, tileAddress, transform, generation);

                        worker.RunAsync(() =>
                        {
                            // Skip any tasks that have been generated for a different generation
                            if (generation == task.Generation)
                            {
                                 //var tileData = new GeoJsonTile(tileAddress, response.data);
                                var mvtTile = new MvtTile(tileAddress, response.data);

                                // Save the tile feature collections in the cache for later use
                                tileCache.Add(tileAddress, mvtTile.FeatureCollections);

                                task.Start(mvtTile.FeatureCollections);

                                tasks.Add(task);
                            }
                        });
                    };

                    // Starts the HTTP request
                    StartCoroutine(tileIO.FetchNetworkData(uri, onTileFetched));
                }
            }
        }

        private PolylineOptions DefaultOptions()
        {
            return new PolylineOptions()
            {
                Enabled = true,
                Extrusion = 0,
                Material = new Material(Shader.Find("Standard")),
                MinHeight = 1f,
                MaxHeight = 1f,
                MiterLimit = 1f,
                Width = 1f
            };
        }
        public void CreateIntersectionTest()
        {
            var polylineOptions = DefaultOptions();

            #region Simple intersection

            var testGo = new GameObject("IntersectionTest");

            var mainHandler = new SplineHandler(new List<Vector2>()
                {
                    new (0, -2),
                    new (0,0), //special case for intersection connection
                    new (0, 2),
                    new (2, 2)
                }, "0001", Matrix4x4.identity, polylineOptions);
            

            var secondHandler = new SplineHandler(new List<Vector2>()
                {
                    new (2, 2),
                    new (4, 2),
                    new (4, 0)
                }, "0002", Matrix4x4.identity, polylineOptions);


            var thirdHandler = new SplineHandler(new List<Vector2>()
            {
                new (0,0),
                new (2,0),
                new (2,-1),
                new (2,-2),

            }, "0003", Matrix4x4.identity, polylineOptions);

            var forthHandler = new SplineHandler(new List<Vector2>()
            {
                new (2,-2),
                new (2,-3),
                new (-1,-3),
                new (-2,-3),
            }, "0004", Matrix4x4.identity, polylineOptions);
            
            var fifthHandler = new SplineHandler(new List<Vector2>()
            {
                new (0,0),
                new (-2,0),
                new (-2,2),
                new (-2,4),
            }, "0005", Matrix4x4.identity, polylineOptions);
            
            var controller = testGo.AddComponent<SplinesController>();
            
            controller.Initialize(new List<SplineHandler>(){mainHandler, secondHandler, thirdHandler,forthHandler, fifthHandler});
            #endregion
            
        }

        public void CreateTjunctionAngleTest()
        {
            #region Angled t-junctions

            var polylineOptions = DefaultOptions();
            var testAngle = new GameObject("AngleTest");

            var taMainHandler = new SplineHandler(new List<Vector2>()
            {
                new (-2, 3),
                new (0,1.75f), 
                new (0.5f, 0),
                new (0, -1.75f),
                new (-2,-3),
            }, "0000", Matrix4x4.identity, polylineOptions);
            
            var firstJunction = new SplineHandler(new List<Vector2>()
            {
                new (0, 1.75f),
                new (0,3),
                new (0,5f),
                new (0, 7),
            }, "0001", Matrix4x4.identity, polylineOptions);
            
            var secondJunction = new SplineHandler(new List<Vector2>()
            {
                new (0, -1.75f),
                new (2,-2),
                new (3, -3),
                new (4, -4),
            }, "0002", Matrix4x4.identity, polylineOptions);

            var thirdJunction = new SplineHandler(new List<Vector2>()
            {
                new(0.5f, 0),
                new(-1, 0),
                new(-3, 0),
                new(-4, 0),
            }, "0003", Matrix4x4.identity, polylineOptions);
            
            var angleController = testAngle.AddComponent<SplinesController>();
            
            angleController.Initialize(new List<SplineHandler>(){taMainHandler, firstJunction, secondJunction, thirdJunction});
            #endregion
        }

        public bool HasPendingTasks()
        {
            return nTasksForArea > 0;
        }

        public bool FinishedRunningTasks()
        {
            // Number of tasks ready for the current generation
            int nTasksReady = 0;

            foreach (var task in tasks)
            {
                if (task.Generation == generation)
                {
                    nTasksReady++;
                }
            }

            return nTasksReady == nTasksForArea;
        }

        [SerializeField] private List<SplineHandler> _handlers;
        public void GenerateSceneGraph()
        {
            if (regionMap != null)
            {
                DestroyImmediate(regionMap);
            }

            // Merge all feature meshes
            List<FeatureMesh> features = new List<FeatureMesh>();
            _handlers = new List<SplineHandler>();
            foreach (var task in tasks)
            {
                if (task.Generation == generation)
                {
                    features.AddRange(task.Data);
                    
                    if(task is SplineTileTask splineTileTask)
                        _handlers.AddRange(splineTileTask.SplineHandlers);
                }
            }

            tasks.Clear();
            nTasksForArea = 0;

            regionMap = new GameObject(RegionName);
            var sceneGraph = new SceneGraph(regionMap, GroupOptions, GameObjectOptions, features);
            
            if(useTomiMerge)
                DrawSplines();
            else
                sceneGraph.Generate();
        }

        private void DrawSplines()
        {
            var splinesParent = new GameObject("Splines");
            splinesParent.transform.SetParent(transform,false);
            var controller = splinesParent.AddComponent<SplinesController>();
            controller.Initialize(_handlers);
        }

        public bool IsValid()
        {
            bool hasStyle = Style != null;
            bool hasApiKey = ApiKey.Length > 0;
            return RegionName.Length > 0 && hasStyle && hasApiKey;
        }

        public void LogWarnings()
        {
            if (ApiKey.Length == 0)
            {
                Debug.LogWarning("Make sure to set an API key in the RegionMap");
            }

            if (Style != null && Style.Layers.Count == 0)
            {
                Debug.LogWarning("The current MapStyle has no layers, no output will be produced");
            }
        }

        public void LogErrors()
        {
            if (RegionName.Length == 0)
            {
                Debug.LogError("Make sure to give a region name");
            }

            if (Style == null)
            {
                Debug.LogError("Make sure to set a MapStyle");
            }
        }

        public void OnValidate()
        {
            if (serializedAssetVersion != currentAssetVersion)
            {
                Debug.LogWarningFormat("The RegionMap \"{0}\" was created with a different version of this tool. " +
                    "Some properties may be missing or have unexpected values.", this.name);
                serializedAssetVersion = currentAssetVersion;
            }
        }
    }
}
