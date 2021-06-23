using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using UnityEngine;

namespace Tomi
{
    public class SplineHandler
    {
        public string Name { get; private set; }
        public bool IsValid => Points is not null && Points.Count >= 2;
        public List<Vector3> Points { get; }
        
        private const float SamePointsMaxDistance = 1f;
        private const float Height = 1;
        private Matrix4x4 _transformMatrix;

        public void Invalidate()
        {
            Points.Clear();
        }
        public SplineHandler(List<Vector2> points, string featureName, Matrix4x4 matrix)
        {
            _transformMatrix = matrix;
            Points = TransformPoints(points, _transformMatrix);
            Name = featureName;
            Debug.Log("Name: "+ Name + " Count:" + Points.Count);
        }
        public SplineHandler(List<Vector3> points, string featureName)
        {
            _transformMatrix = Matrix4x4.identity;
            Points = points;
            Name = featureName;
            Debug.Log("Name: "+ Name + " Count:" + Points.Count);
        }
        
        #region JOIN

      
        //TODO: Try with backwards faced splines (last + last) ? 
        public bool CanJoinWith(SplineHandler otherSpline)
        {
            if (!IsValid || !otherSpline.IsValid)
                return false;
           
            var otherFirst = otherSpline.Points[0];
            var otherLast = otherSpline.Points[otherSpline.Points.Count - 1];
           
            return isSamePoint(otherFirst, Points[Points.Count - 1]) || isSamePoint(otherLast, Points[0]);
        }

        public bool Join(SplineHandler otherSpline)
        {
            if (!CanJoinWith(otherSpline))
                return false;

            var otherFirst = otherSpline.Points[0];
            var otherLast = otherSpline.Points[otherSpline.Points.Count - 1];

            if (isSamePoint(otherFirst, Points[Points.Count - 1]))
            {
                Points.RemoveAt(Points.Count - 1);
                Points.AddRange(otherSpline.Points);
                otherSpline.Invalidate();
                return true;
            }

            if (isSamePoint(otherLast, Points[0]))
            {
                Points.RemoveAt(0);
                Points.InsertRange(0, otherSpline.Points);
                otherSpline.Invalidate();
                return true;
            }
            
            return false;
    }

        private bool isSamePoint(Vector3 v1, Vector3 v2)
        {
            return Vector3.Distance(v1, v2) < Mathf.Abs(SamePointsMaxDistance);
        }

        #endregion

        private List<Vector3> TransformPoints(List<Vector2> points, Matrix4x4 matrix4X4)
        {
            var transformedPoints = new List<Vector3>();
            
            foreach (var point in points)
            {
                var p = matrix4X4.MultiplyPoint3x4(new Vector3(point.x,Height,point.y));
                transformedPoints.Add(p);
            }

            return transformedPoints;
        }

        public void Build(Transform parent)
        {
            if (Points.Count < 2)
                return;
            
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