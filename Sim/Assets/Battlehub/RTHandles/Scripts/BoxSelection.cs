﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

namespace Battlehub.RTHandles
{
    public enum BoxSelectionMethod
    {
        LooseFitting,
        BoundsCenter,
        TansformCenter
    }

    public class FilteringArgs : EventArgs
    {
        private bool m_cancel;

        public bool Cancel
        {
            get { return m_cancel; }
            set
            {
                if (value) //can't reset cancel flag
                {
                    m_cancel = true;
                }
            }
        }

        public GameObject Object
        {
            get;
            set;
        }

        public void Reset()
        {
            m_cancel = false;
        }
    }

    public class BoxSelectionArgs
    {
        public GameObject[] GameObjects;
    }
    

    public interface IBoxSelection
    {
        event EventHandler<FilteringArgs> Filtering;
        event EventHandler<BoxSelectionArgs> Selection;
        bool IsDragging
        {
            get;
        }
        Bounds SelectionBounds
        {
            get;
        }
    }

    /// <summary>
    /// Box Selection
    /// </summary>
    public class BoxSelection : RTEComponent, IBoxSelection
    {
        public Sprite Graphics;
        protected Image m_image;
        protected RectTransform m_rectTransform;
        protected Canvas m_canvas;
        protected bool m_isDragging;
        protected Vector3 m_startMousePosition;
        protected Vector2 m_startPt;
        protected Vector2 m_endPt;

        public bool UseCameraSpace = true;
        public BoxSelectionMethod Method;
       
        public event EventHandler<FilteringArgs> Filtering;
        public event EventHandler<BoxSelectionArgs> Selection;

        public Bounds SelectionBounds
        {
            get;
            private set;
        }

        public bool IsDragging
        {
            get { return m_isDragging; }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            Window.IOCContainer.RegisterFallback<IBoxSelection>(this);
            if (m_canvas == null)
            {
                GameObject go = new GameObject("BoxSelection");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform.parent, false);
                m_canvas = go.AddComponent<Canvas>();
            }
            if (UseCameraSpace)
            {
                m_canvas.worldCamera = Window.Camera;
                m_canvas.renderMode = RenderMode.ScreenSpaceCamera;
                m_canvas.planeDistance = Window.Camera.nearClipPlane + 0.05f;
                m_canvas.transform.rotation = Quaternion.identity;
                m_canvas.transform.position = Vector3.zero;
            }
            else
            {
                m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            transform.SetParent(m_canvas.gameObject.transform, false);
            transform.rotation = Quaternion.identity;
            transform.position = Vector3.zero;

            CanvasScaler scaler = m_canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = m_canvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.referencePixelsPerUnit = 1;
            

            if(!GetComponent<BoxSelectionInput>())
            {
                gameObject.AddComponent<BoxSelectionInput>();
            }

            m_rectTransform = GetComponent<RectTransform>();
            if (m_rectTransform == null)
            {
                m_rectTransform = gameObject.AddComponent<RectTransform>();
            }
            m_rectTransform.sizeDelta = new Vector2(0, 0);
            m_rectTransform.pivot = new Vector2(0, 0);
            m_rectTransform.anchoredPosition = new Vector3(0, 0);

            m_image = gameObject.AddComponent<Image>();
            m_image.type = Image.Type.Sliced;
            if (Graphics == null)
            {
                Graphics = Resources.Load<Sprite>("RTH_BoxSelection");
            }
            m_image.sprite = Graphics;
            m_image.raycastTarget = false;
        }


        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            Window.IOCContainer.UnregisterFallback<IBoxSelection>(this);
            if (Editor != null && Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                if (Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }
        }

        protected override void OnWindowDeactivated()
        {
            base.OnWindowDeactivated();
            if (Editor != null && Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                if (Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }
        }

        public void BeginSelect()
        {
            if(Editor.Tools.ActiveTool != null)
            {
                return;
            }

            m_startMousePosition = Window.Pointer.ScreenPoint;
            m_isDragging = GetPoint(out m_startPt) && (!Window.Editor.IsOpened || Window.IsPointerOver);
            if (m_isDragging)
            {
                m_rectTransform.anchoredPosition = m_startPt;
                m_rectTransform.sizeDelta = new Vector2(0, 0);
            }
        }

        public void EndSelect()
        {
            if (m_isDragging)
            {
                m_isDragging = false;

                HitTest();
                m_rectTransform.sizeDelta = new Vector2(0, 0);
                if (Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }
        }

        private void Update()
        {
            if (!Editor.Selection.Enabled)
            {
                return;
            }

            if (Editor.Tools.ActiveTool != this && Editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (Editor.Tools.IsViewing)
            {
                if(Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
                
                m_isDragging = false;
                m_rectTransform.sizeDelta = new Vector2(0, 0);
                return;
            }

            if (m_isDragging)
            {
                GetPoint(out m_endPt);

                Vector2 size = m_endPt - m_startPt;
                Editor.Tools.ActiveTool = this;
                if (size != Vector2.zero)
                {
                    Editor.Tools.ActiveTool = this;
                }
                m_rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                m_rectTransform.localScale = new Vector3(Mathf.Sign(size.x), Mathf.Sign(size.y), 1);
            }
        }

        private void HitTest()
        {
            if (m_rectTransform.sizeDelta.magnitude < 5f)
            {
                return;
            }

            Vector3 center = (m_startMousePosition + (Vector3)Window.Pointer.ScreenPoint) / 2;
            center.z = 0.0f;
            Bounds selectionBounds = new Bounds(center, m_rectTransform.sizeDelta);
            SelectionBounds = selectionBounds;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Window.Camera);
            
            HashSet<GameObject> selection = new HashSet<GameObject>();
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            Collider[] colliders = FindObjectsOfType<Collider>();
            FilteringArgs args = new FilteringArgs();
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer r = renderers[i];
                Bounds bounds = r.bounds;
                GameObject go = r.gameObject;
                TrySelect(ref selectionBounds, selection, args, ref bounds, go, frustumPlanes);
            }
            for (int i = 0; i < colliders.Length; ++i)
            {
                Collider c = colliders[i];
                Bounds bounds = c.bounds;
                GameObject go = c.gameObject;
                TrySelect(ref selectionBounds, selection, args, ref bounds, go, frustumPlanes);
            }

            if(Selection != null)
            {
                Selection(this, new BoxSelectionArgs { GameObjects = selection.ToArray() });
            }
            else
            {
                Editor.Selection.objects = selection.ToArray();
            }
        }

        private void TrySelect(ref Bounds selectionBounds, HashSet<GameObject> selection, FilteringArgs args, ref Bounds bounds, GameObject go, Plane[] frustumPlanes)
        {
            bool select;
            if (Method == BoxSelectionMethod.LooseFitting)
            {
                select = LooseFitting(ref selectionBounds, ref bounds);
            }
            else if (Method == BoxSelectionMethod.BoundsCenter)
            {
                select = BoundsCenter(ref selectionBounds, ref bounds);
            }
            else
            {
                select = TransformCenter(ref selectionBounds, go.transform);
            }

            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                select = false;
            }

            if (select)
            {
                if (!selection.Contains(go))
                {
                    if (Filtering != null)
                    {
                        args.Object = go;
                        Filtering(this, args);
                        if (!args.Cancel)
                        {
                            selection.Add(go);
                        }
                        args.Reset();
                    }
                    else
                    {
                        selection.Add(go);
                    }
                }
            }
        }

        private bool TransformCenter(ref Bounds selectionBounds, Transform tr)
        {
            Vector3 screenPoint = Window.Camera.WorldToScreenPoint(tr.position);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool BoundsCenter(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 screenPoint = Window.Camera.WorldToScreenPoint(bounds.center);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool LooseFitting(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 p0 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p1 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p2 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p3 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
            Vector3 p4 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p5 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p6 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p7 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            p0 = Window.Camera.WorldToScreenPoint(p0);
            p1 = Window.Camera.WorldToScreenPoint(p1);
            p2 = Window.Camera.WorldToScreenPoint(p2);
            p3 = Window.Camera.WorldToScreenPoint(p3);
            p4 = Window.Camera.WorldToScreenPoint(p4);
            p5 = Window.Camera.WorldToScreenPoint(p5);
            p6 = Window.Camera.WorldToScreenPoint(p6);
            p7 = Window.Camera.WorldToScreenPoint(p7);

            float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            Vector3 min = new Vector2(minX, minY);
            Vector3 max = new Vector2(maxX, maxY);

            Bounds b = new Bounds((min + max) / 2, (max - min));
            return selectionBounds.Intersects(b);
        }

        private bool GetPoint(out Vector2 localPoint)
        {
            Camera cam = null;
            if(m_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = m_canvas.worldCamera;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.GetComponent<RectTransform>(), Window.Pointer.ScreenPoint, cam, out localPoint);
        }
    }

}
