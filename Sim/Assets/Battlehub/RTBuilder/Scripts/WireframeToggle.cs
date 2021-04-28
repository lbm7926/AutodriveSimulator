using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class WireframeToggle : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_toggle = null;

        private void Start()
        {
            m_toggle.isOn = GetComponent<Wireframe>();
            m_toggle.onValueChanged.AddListener(OnWireframeToggleValueChanged);
        }

        private void OnDestroy()
        {
            if(m_toggle != null)
            {
                m_toggle.onValueChanged.RemoveListener(OnWireframeToggleValueChanged);
            }
        }

        private void OnWireframeToggleValueChanged(bool value)
        {
            Wireframe wireframe = GetComponent<Wireframe>();
            if (!value)
            {
                if(wireframe)
                {
                    Destroy(wireframe);
                }   
            }
            else
            {
                if(!wireframe)
                {
                    gameObject.AddComponent<Wireframe>();
                }
            }
        }
    }

}
