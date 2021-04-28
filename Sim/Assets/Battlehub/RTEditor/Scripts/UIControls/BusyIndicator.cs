using UnityEngine;

namespace Battlehub.RTEditor
{
    public class BusyIndicator : MonoBehaviour
    {
        [SerializeField]
        private Transform m_graphics;

        [SerializeField]
        private float m_interval = 0.2f;

        private float m_nextT;

        private void Awake()
        {
            m_graphics = transform;
        }

        private void Update()
        {
            if(m_nextT <= Time.time)
            {
                m_nextT = Time.time + m_interval;
                m_graphics.Rotate(Vector3.forward, -30);
            }
        }
    }
}

