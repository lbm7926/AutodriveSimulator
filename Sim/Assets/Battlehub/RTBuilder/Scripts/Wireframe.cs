using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTSL;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public class Wireframe : MonoBehaviour
    {
        private IRTE m_editor;
        private RuntimeWindow m_window;

        private void Awake()
        {
            m_window = GetComponent<RuntimeWindow>();
            
            m_editor = IOC.Resolve<IRTE>();
            m_editor.Object.Started += OnObjectStarted;

            foreach (ExposeToEditor obj in m_editor.Object.Get(false))
            {
                PBMesh pbMesh = obj.GetComponent<PBMesh>();
                if (pbMesh != null)
                {
                    CreateWireframeMesh(pbMesh);
                }
            }
        }

        private void Start()
        {
            SetCullingMask(m_window);
        }

        private void OnDestroy()
        {
            if(m_editor != null && m_editor.Object != null)
            {
                m_editor.Object.Started -= OnObjectStarted;

                foreach (ExposeToEditor obj in m_editor.Object.Get(false))
                {
                    PBMesh pbMesh = obj.GetComponent<PBMesh>();
                    if (pbMesh != null)
                    {
                        WireframeMesh[] wireframeMesh = pbMesh.GetComponentsInChildren<WireframeMesh>(true);
                        for(int i = 0; i < wireframeMesh.Length; ++i)
                        {
                            WireframeMesh wireframe = wireframeMesh[i];
                            if (!wireframe.IsIndividual)
                            {
                                Destroy(wireframe.gameObject);
                                break;
                            }
                        }
                    }
                }
            }

            if(m_window != null)
            {
                ResetCullingMask(m_window);
            }
        }

        private void OnObjectStarted(ExposeToEditor obj)
        {
            PBMesh pbMesh = obj.GetComponent<PBMesh>();
            if(pbMesh != null)
            {
                CreateWireframeMesh(pbMesh);
            }
        }

        private void CreateWireframeMesh(PBMesh pbMesh)
        {
            GameObject wireframe = new GameObject("Wireframe");
            wireframe.transform.SetParent(pbMesh.transform, false);

            wireframe.hideFlags = HideFlags.DontSave;
            wireframe.layer = m_editor.CameraLayerSettings.ExtraLayer;
            wireframe.AddComponent<WireframeMesh>();
        }

        private void SetCullingMask(RuntimeWindow window)
        {
            window.Camera.cullingMask = (1 << LayerMask.NameToLayer("UI")) | (1 << m_editor.CameraLayerSettings.AllScenesLayer) | (1 << m_editor.CameraLayerSettings.ExtraLayer);
            window.Camera.backgroundColor = Color.white;
            window.Camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void ResetCullingMask(RuntimeWindow window)
        {
            CameraLayerSettings settings = m_editor.CameraLayerSettings;
            window.Camera.cullingMask = ~((1 << m_editor.CameraLayerSettings.ExtraLayer) | ((1 << settings.MaxGraphicsLayers) - 1) << settings.RuntimeGraphicsLayer);
            window.Camera.clearFlags = CameraClearFlags.Skybox;
        }

    }

}

