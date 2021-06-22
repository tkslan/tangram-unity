using System.Collections;
using System.Collections.Generic;
using BezierSolution;
using UnityEngine;

namespace Tomi
{
    public class SplineHandler
    {
        public string Name { get; private set; }
        public Matrix4x4 TransformMatrix { get; private set; }
        public List<Vector3> Points { get; }
        private const float Height = 1;

        public SplineHandler(List<Vector2> points, string featureName, Matrix4x4 matrix)
        {
            Points = TransformPoints(points, matrix);
            TransformMatrix = matrix;
            Name = featureName;
            Debug.Log("Name: "+ Name + " Count:" + Points.Count);
        }

        private List<Vector3> TransformPoints(List<Vector2> points, Matrix4x4 matrix4X4)
        {
            var transformedPoints = new List<Vector3>();
            
            foreach (var point in points)
            {
                var p = matrix4X4.MultiplyPoint(new Vector3(point.x,Height,point.y));
                transformedPoints.Add(p);
            }

            return transformedPoints;
        }

        public void Build(Transform parent)
        {
            var splineObject = new GameObject(Name);
            splineObject.transform.SetParent(parent, false);
            var spline = splineObject.AddComponent<BezierSpline>();
            spline.drawGizmos = true;
            spline.gizmoColor = Color.green;
            spline.Initialize(Points.Count);
            for (var index = 0; index < spline.Count; index++)
            {
                var p = spline[index];
                p.position = Points[index];
                p.Refresh();
            }
            
            spline.ConstructLinearPath();
        }
    }
}