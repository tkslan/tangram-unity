using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Mapzen.Unity.Editor
{
    [CustomEditor(typeof(RegionMap))]
    public class RegionMapEditor : UnityEditor.Editor
    {
        private RegionMap map;

        void OnEnable()
        {
            this.map = (RegionMap)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ApiKey"));
            if (GUILayout.Button("Get an API key", EditorStyles.miniButtonRight))
            {
                Application.OpenURL("https://developers.nextzen.org/");
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Style"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useTomiMerge"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Area"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("UnitsPerMeter"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("RegionName"));

            map.GroupOptions = (SceneGroupType)EditorGUILayout.EnumFlagsField("GroupingOptions", map.GroupOptions);

            // EditorGUILayout.PropertyField(serializedObject.FindProperty("GroupOptions"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("GameObjectOptions"), true);

            bool valid = map.IsValid();

            EditorConfig.SetColor(valid ?
                EditorConfig.DownloadButtonEnabledColor :
                EditorConfig.DownloadButtonDisabledColor);

            if (GUILayout.Button("Download"))
            {
                map.LogWarnings();

                if (valid)
                {
                    map.DownloadTilesAsync();
                }
                else
                {
                    map.LogErrors();
                }
            }

            if (GUILayout.Button("Intersection test"))
            {
                map.CreateIntersectionTest();
            }

            if (GUILayout.Button("T junction angles test"))
            {
                map.CreateTjunctionAngleTest();
            }

            if (map.HasPendingTasks())
            {
                // Go through another OnInspectorGUI cycle
                Repaint();

                if (map.FinishedRunningTasks())
                {
                    map.GenerateSceneGraph();
                }
            }

            EditorConfig.ResetColor();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
