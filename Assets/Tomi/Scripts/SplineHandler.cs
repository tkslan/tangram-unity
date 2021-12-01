using System.Collections.Generic;
using BezierSolution;
using Mapzen.Unity;
using UnityEngine;
using UnityEngine.Assertions;
using Tomi.Geometry;
using Tomi.Intersection;

namespace Tomi
{
    public class SplineHandler
    {
        public string Name { get; private set; }
        public bool IsValid => Points is not null && Points.Count >= 2;
        public List<Vector3> Points { get; }
        
        public SplineMeshBuilder Builder { get; }
        
        public PolylineOptions PolylineOptions => _polylineOptions;
        public Matrix4x4 Matrix => _transformMatrix;
        
        private const float SamePointsMaxDistance = 0.5f;
        private const float Height = 1;
        
        private readonly Matrix4x4 _transformMatrix;
        private readonly PolylineOptions _polylineOptions;
        public void Invalidate()
        {
            Points.Clear();
        }
     
        public SplineHandler(List<Vector2> points, string featureName, Matrix4x4 matrix, PolylineOptions polylineOptions)
        {
            _transformMatrix = matrix;
            _polylineOptions = polylineOptions;
            Points = TransformPoints(points, _transformMatrix);
            Name = featureName;
            Builder = new SplineMeshBuilder(this);
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
            
            return isSamePoint(otherSpline.Points[0], Points[Points.Count - 1]) || isSamePoint(otherSpline.Points[otherSpline.Points.Count - 1], Points[0]);
        }

        
        public bool IsConnectedWith(SplineHandler otherSpline, out ConnectionPoint connectionPoint)
        {
            connectionPoint = new ConnectionPoint(Vector2.zero);
            
            if (!IsValid || !otherSpline.IsValid)
                return false;
            
            var otherFirst = otherSpline.Points[0];
            var otherLastIndex = otherSpline.Points.Count - 1;
            var otherLast = otherSpline.Points[otherLastIndex];
            for (var index = 0; index < Points.Count; index++)
            {
                var point = Points[index];
                
                if (isSamePoint(point, otherFirst))
                {
                    connectionPoint = new ConnectionPoint(point, index, 0,this, otherSpline);
                    return true;
                }
                
                if (isSamePoint(point, otherLast))
                {
                    connectionPoint = new ConnectionPoint(point, index, otherLastIndex,this, otherSpline);
                    return true;
                }
            }

            return false;
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

        public void Build(Transform parent, bool withSpline = true)
        {
            if (Points.Count < 2)
                return;

            if (withSpline)
                BuildSpline(parent);
            
            if (Builder.GameObject != null)
                Object.DestroyImmediate(Builder.GameObject);
            
            Builder.Build(parent);
            Assert.IsNotNull(Builder.Mesh);
            Assert.IsNotNull(Builder.GameObject);
        }

        private void BuildSpline(Transform parent)
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