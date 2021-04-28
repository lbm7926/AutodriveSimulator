using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class BaseViewInput<T> : MonoBehaviour where T : RuntimeWindow
    {
        public KeyCode RuntimeModifierKey = KeyCode.LeftControl;
        public KeyCode EditorModifierKey = KeyCode.LeftShift;
        public KeyCode ModifierKey
        {
            get
            {
                #if UNITY_EDITOR
                return EditorModifierKey;
                #else
                return RuntimeModifierKey;
                #endif
            }
        }
        public KeyCode SelectAllKey = KeyCode.A;
        public KeyCode DuplicateKey = KeyCode.D;
        public KeyCode DeleteKey = KeyCode.Delete;
        protected virtual bool SelectAllAction()
        {
            return Input.GetKeyDown(SelectAllKey) && Input.GetKey(ModifierKey);
        }
        protected virtual bool DuplicateAction()
        {
            return Input.GetKeyDown(DuplicateKey) && Input.GetKey(ModifierKey);
        }
        protected virtual bool DeleteAction()
        {
            return Input.GetKeyDown(DeleteKey);
        }

        private T m_window;
        protected T View
        {
            get { return m_window; }
        }

        private IRTE m_editor;
        protected IRTE Editor
        {
            get { return m_editor; }
        }

        private IInput m_input;
        protected IInput Input
        {
            get { return m_input; }
        }

        private IWindowManager m_wm;

        private void Start()
        {
            m_window = GetComponent<T>();
            m_editor = m_window.Editor;
            m_input = m_editor.Input;
            m_wm = IOC.Resolve<IWindowManager>();
            StartOverride();
        }

        protected virtual void StartOverride()
        {

        }

        private void Update()
        {
            if (m_window.Editor.ActiveWindow != m_window || m_editor.IsInputFieldActive || (m_wm != null && m_wm.IsDialogOpened))
            {
                return;
            }
            UpdateOverride();
        }

        protected virtual void UpdateOverride()
        {

        }

       
    }
}
