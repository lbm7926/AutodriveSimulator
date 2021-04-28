using Battlehub.RTCommon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.Spline3
{
    public class PickResult
    {
        public GameObject Spline;
        public BaseSpline GetSpline()
        {
            BaseSpline[] splines =  Spline.GetComponents<BaseSpline>();
            return splines.Where(s => s.IsSelectable).FirstOrDefault();
        }
        public float ScreenDistance;
        public Vector3 WorldPosition;
        public int Index;

        public PickResult()
        {

        }

        public PickResult(PickResult other)
        {
            Spline = other.Spline;
            ScreenDistance = other.ScreenDistance;
            WorldPosition = other.WorldPosition;
            Index = other.Index;
        }
    }

    [DefaultExecutionOrder(-1)]
    public class ControlPointPicker : MonoBehaviour
    {
        private IRTE m_editor;
        private bool m_isControlPointSelected;
        private PickResult m_pickResult;
        private Vector3 m_prevPosition;

        public bool IsControlPointSelected
        {
            get { return m_isControlPointSelected; }
        }
        
        public PickResult Selection
        {
            get { return m_pickResult; }
            set
            {
                m_pickResult = value;
                if(m_pickResult != null)
                {
                    transform.position = m_pickResult.GetSpline().GetControlPoint(m_pickResult.Index);
                }
            }
        }

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_editor.Selection.SelectionChanged += OnSelectionChanged;
            m_prevPosition = transform.position;
        }

        private void OnDestroy()
        {
            if(m_editor != null)
            {
                m_editor.Selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void LateUpdate()
        {
            if(m_pickResult == null)
            {
                return;
            }

            if(m_prevPosition != transform.position)
            {
                m_prevPosition = transform.position;
                BaseSpline spline = m_pickResult.GetSpline();
                spline.SetControlPoint(m_pickResult.Index, transform.position);
            }
        }

        public void Drag(bool extend)
        {
            BaseSpline spline = m_pickResult.GetSpline();
            if (extend)
            {
                if (m_pickResult.Index == 1 || m_pickResult.Index == 0)
                {
                    spline.Prepend();
                }
                else if (m_pickResult.Index == spline.SegmentsCount + 1 ||
                        m_pickResult.Index == spline.SegmentsCount + 2)
                {
                    spline.Append();
                    m_pickResult.Index++;
                }
            }

            spline.SetControlPoint(m_pickResult.Index, transform.position);
            m_prevPosition = transform.position;
        }

        public void Pick(Camera camera, Vector2 position)
        {
            m_pickResult = PickControlPoint(camera, position, 20);
            if (m_pickResult != null)
            {
                BaseSpline spline = m_pickResult.GetSpline();
                transform.position = spline.GetControlPoint(m_pickResult.Index);
                m_editor.Selection.activeGameObject = gameObject;
            }
        }

        public void Append()
        {
            BaseSpline spline = m_pickResult.GetSpline();
            spline.Append(2.0f);
            m_pickResult.Index = spline.LocalControlPoints.Count() - 1;
            transform.position = spline.GetControlPoint(m_pickResult.Index);
        }

        public void Prepend()
        {
            BaseSpline spline = m_pickResult.GetSpline();
            spline.Prepend(2.0f);
            m_pickResult.Index = 0;
            transform.position = spline.GetControlPoint(m_pickResult.Index);
        }

        public void Remove()
        {
            BaseSpline spline = m_pickResult.GetSpline();
            spline.Remove(m_pickResult.Index);
            if (spline.SegmentsCount <= m_pickResult.Index - 1)
            {
                m_pickResult.Index--;
            }
            transform.position = spline.GetControlPoint(m_pickResult.Index);
        }

        private void OnSelectionChanged(Object[] unselectedObjects)
        {
            m_isControlPointSelected = m_editor.Selection.activeGameObject == gameObject;
            if(!m_isControlPointSelected)
            {
                m_pickResult = null;
            }
        }

       

        private List<PickResult> m_nearestControlPoints = new List<PickResult>();
        public PickResult PickControlPoint(Camera camera, Vector3 mousePosition, float maxDistance)
        {
            BaseSpline[] splines = FindObjectsOfType<BaseSpline>();

            m_nearestControlPoints.Clear();
            maxDistance = maxDistance * maxDistance;

            foreach (BaseSpline spline in splines)
            {
                if(!spline.IsSelectable)
                {
                    continue;
                }
                GetNearestVertices(camera, spline, mousePosition, m_nearestControlPoints, maxDistance, 1.0f);
            }

            if (m_nearestControlPoints.Count == 0)
            {
                return null;
            }

            PickResult result = m_nearestControlPoints[0];
            for (int i = 1; i < m_nearestControlPoints.Count; i++)
            {
                if (m_nearestControlPoints[i].ScreenDistance < result.ScreenDistance) result = m_nearestControlPoints[i];
            }

            m_nearestControlPoints.Clear();
            return result;
        }

        private int GetNearestVertices(Camera camera, BaseSpline spline, Vector3 mousePosition, List<PickResult> list, float maxDistance, float distModifier)
        {
            IEnumerable<Vector3> points = spline.LocalControlPoints;

            int index = 0;
            int matches = 0;
            foreach (Vector3 point in points)
            {
                Vector3 v = spline.transform.TransformPoint(point);
                Vector3 p = camera.WorldToScreenPoint(v);
                p.z = mousePosition.z;

                float dist = (p - mousePosition).sqrMagnitude * distModifier;

                if (dist < maxDistance)
                {
                    list.Add(new PickResult
                    {
                        Spline = spline.gameObject,
                        ScreenDistance = dist,
                        WorldPosition = v,
                        Index = index
                    });

                    matches++;
                }

                index++;
            }

            return matches;
        }
    }
}


