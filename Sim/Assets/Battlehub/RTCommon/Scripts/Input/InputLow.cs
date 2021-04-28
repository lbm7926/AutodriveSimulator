using UnityEngine;

namespace Battlehub.RTCommon
{
    public enum InputAxis
    {
        X,
        Y,
        Z
    }

    public interface IInput
    {
        MultitouchEmulator MultitouchEmulator
        {
            get;
            set;
        }

        bool IsAnyKeyDown();

        float GetAxis(InputAxis axis);
        bool GetKeyDown(KeyCode key);
        bool GetKeyUp(KeyCode key);
        bool GetKey(KeyCode key);
        
        Vector3 GetPointerXY(int pointer);

        bool GetPointerDown(int button);
        bool GetPointerUp(int button);
        bool GetPointer(int button);
    }

    public class InputLowVR : InputLow
    {
        public override float GetAxis(InputAxis axis)
        {
            switch (axis)
            {
                case InputAxis.X:
                    return Input.GetAxis("Horizontal");
                case InputAxis.Y:
                    return 0;
                case InputAxis.Z:
                    return Input.GetAxis("Vertical");
                default:
                    return 0;
            }
        }

        public override Vector3 GetPointerXY(int pointer)
        {
            return Vector3.zero;
        }

        public override bool GetPointerDown(int index)
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        public override bool GetPointerUp(int index)
        {
            return Input.GetKeyUp(KeyCode.Space);
        }

        public override bool GetPointer(int index)
        {
            return Input.GetKey(KeyCode.Space);
        }
    }

    public class DisabledInput : IInput
    {
        public MultitouchEmulator MultitouchEmulator { get { return null; } set { } }

        public float GetAxis(InputAxis axis)
        {
            return 0;
        }

        public bool GetKey(KeyCode key)
        {
            return false;
        }

        public bool GetKeyDown(KeyCode key)
        {
            return false;
        }

        public bool GetKeyUp(KeyCode key)
        {
            return false;
        }

        public bool GetPointer(int button)
        {
            return false;
        }

        public bool GetPointerDown(int button)
        {
            return false;
        }

        public bool GetPointerUp(int button)
        {
            return false;
        }

        public Vector3 GetPointerXY(int pointer)
        {
            if (pointer == 0)
            {
                return Input.mousePosition;
            }
            else
            {
                Touch touch = Input.GetTouch(pointer);
                return touch.position;
            }
        }

        public bool IsAnyKeyDown()
        {
            return false;
        }
    }


    public class InputLow : IInput
    {
        private MultitouchEmulator m_multitouchEmulator;
        public MultitouchEmulator MultitouchEmulator
        {
            get { return m_multitouchEmulator; }
            set { m_multitouchEmulator = value; }
        }

        public virtual bool IsAnyKeyDown()
        {
            return Input.anyKeyDown;
        }

        public virtual bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public virtual bool GetKeyUp(KeyCode key)
        {
            return Input.GetKeyUp(key);
        }

        public virtual bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public virtual float GetAxis(InputAxis axis)
        {
            switch (axis)
            {
                case InputAxis.X:
                    return Input.GetAxis("Mouse X");
                case InputAxis.Y:
                    return Input.GetAxis("Mouse Y");
                case InputAxis.Z:
                    return Input.GetAxis("Mouse ScrollWheel");
                default:
                    return 0;
            }
        }

        public virtual Vector3 GetPointerXY(int pointer)
        {
//#if DEBUG
//            if(m_multitouchEmulator != null)
//            {
//                return m_multitouchEmulator.GetPosition(pointer);
//            }
//#endif
            if (pointer == 0)
            {
                return Input.mousePosition;
            }
            else
            {
                Touch touch = Input.GetTouch(pointer);
                return touch.position;
            }
        }

        public virtual bool GetPointerDown(int index)
        {
//#if DEBUG
//            if (m_multitouchEmulator != null)
//            {
//                return m_multitouchEmulator.IsTouchDown(index);
//            }
//#endif

            bool buttonDown = Input.GetMouseButtonDown(index);
            return buttonDown;
        }

        public virtual bool GetPointerUp(int index)
        {
//#if DEBUG
//            if (m_multitouchEmulator != null)
//            {
//                return m_multitouchEmulator.IsTouchUp(index);
//            }
//#endif
            return Input.GetMouseButtonUp(index);
        }

        public virtual bool GetPointer(int index)
        {
//#if DEBUG
//            if (m_multitouchEmulator != null)
//            {
//                return m_multitouchEmulator.IsTouch(index);
//            }
//#endif

            return Input.GetMouseButton(index);
        }
    }
}
