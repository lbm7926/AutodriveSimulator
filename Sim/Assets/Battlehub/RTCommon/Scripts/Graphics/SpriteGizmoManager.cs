using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTCommon
{
    public class SpriteGizmoManager : MonoBehaviour
    {
        private static readonly Dictionary<Type, string> m_typeToMaterialName = new Dictionary<Type, string>
            {
                {  typeof(Light), "BattlehubLightGizmo" },
                {  typeof(Camera), "BattlehubCameraGizmo" },
                {  typeof(AudioSource), "BattlehubAudioSourceGizmo" }
            };

        private static Dictionary<Type, Material> m_typeToMaterial;
        private static Type[] m_types;
        private IRTE m_editor;

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            if(m_editor == null)
            {
                Debug.LogError("RTE is null");
            }


            AwakeOverride();
        }

        private void Start()
        {
            Cleanup();
            Initialize();

            StartOverride();
        }

        private void OnDestroy()
        {
            Cleanup();

            m_typeToMaterial = null;
            m_types = null;

            OnDestroyOverride();
        }

        protected virtual void AwakeOverride()
        {

        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void OnDestroyOverride()
        {

        }

        protected virtual Type[] GetTypes(Type[] types)
        {
            return types;
        }
        
        protected virtual void GreateGizmo(GameObject go, Type type)
        {
            Material material;
            if (m_typeToMaterial.TryGetValue(type, out material))
            {
                SpriteGizmo gizmo = go.GetComponent<SpriteGizmo>();
                if (!gizmo)
                {
                    gizmo = go.AddComponent<SpriteGizmo>();
                }

                gizmo.Material = material;
            }
        }

        protected virtual void DestroyGizmo(GameObject go)
        {
            SpriteGizmo gizmo = go.GetComponent<SpriteGizmo>();
            if (gizmo)
            {
                Destroy(gizmo);
            }
        }

        private void Initialize()
        {
            if (m_types != null)
            {
                Debug.LogWarning("Already initialized");
                return;
            }

            m_typeToMaterial = new Dictionary<Type, Material>();
            foreach (KeyValuePair<Type, string> kvp in m_typeToMaterialName)
            {
                Material material = Resources.Load<Material>(kvp.Value);
                if (material != null)
                {
                    m_typeToMaterial.Add(kvp.Key, material);
                }
            }

            int index = 0;
            m_types = new Type[m_typeToMaterial.Count];
            foreach (Type type in m_typeToMaterial.Keys)
            {
                m_types[index] = type;
                index++;
            }

            m_types = GetTypes(m_types);
            OnIsOpenedChanged();
            m_editor.IsOpenedChanged += OnIsOpenedChanged;
        }

        private void Cleanup()
        {
            m_types = null;
            m_typeToMaterial = null;
            if(m_editor != null)
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
            }
            UnsubscribeAndDestroy();
        }

        private void UnsubscribeAndDestroy()
        {
            Unsubscribe();

            SpriteGizmo[] objs = Resources.FindObjectsOfTypeAll<SpriteGizmo>();
            for (int j = 0; j < objs.Length; ++j)
            {
                SpriteGizmo obj = objs[j];
                if (!obj.gameObject.IsPrefab())
                {
                    DestroyGizmo(obj.gameObject);
                }
            }
        }

        private void OnIsOpenedChanged()
        {
            if (m_editor.IsOpened)
            {
                IEnumerable<ExposeToEditor> objects = m_editor.Object.Get(false);

                for (int i = 0; i < m_types.Length; ++i)
                {
                    IEnumerable<ExposeToEditor> objectsOfType = objects.Where(o => o.GetComponent(m_types[i]) != null);
                    foreach (ExposeToEditor obj in objectsOfType)
                    {
                        GreateGizmo(obj.gameObject, m_types[i]);
                    }
                }

                Subscribe();
            }
            else
            {
                UnsubscribeAndDestroy();
            }
        }

        private void Subscribe()
        {
            m_editor.Object.Awaked += OnAwaked;
            m_editor.Object.Destroyed += OnDestroyed;
        }

        private void Unsubscribe()
        {
            if(m_editor != null && m_editor.Object != null)
            {
                m_editor.Object.Awaked -= OnAwaked;
                m_editor.Object.Destroyed -= OnDestroyed;
            }
        }

        private void OnAwaked(ExposeToEditor obj)
        {
            for (int i = 0; i < m_types.Length; ++i)
            {
                Component component = obj.GetComponent(m_types[i]);
                if (component != null)
                {
                    GreateGizmo(obj.gameObject, m_types[i]);
                }
            }
        }

        private void OnDestroyed(ExposeToEditor obj)
        {
            for (int i = 0; i < m_types.Length; ++i)
            {
                Component component = obj.GetComponent(m_types[i]);
                if (component != null)
                {
                    DestroyGizmo(obj.gameObject);
                }
            }
        }
    }

}
