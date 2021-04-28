using Battlehub.RTCommon;
using System;
using System.Collections;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class EditorOverride : MonoBehaviour
    {
        private IRTEState m_rteState;
        private IRTE m_editor;

        protected virtual void Awake()
        {
            m_rteState = IOC.Resolve<IRTEState>();
            if (m_rteState != null)
            {
                if (m_rteState.IsCreated)
                {
                    OnEditorExist();
                }
                else
                {
                    m_rteState.Created += OnEditorCreated;
                }
            }
            else
            {
                OnEditorExist();
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_rteState != null)
            {
                m_rteState.Created -= OnEditorCreated;
            }

            if(m_editor != null)
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
            }
        }

        protected virtual void OnEditorExist()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_editor.IsOpenedChanged += OnIsOpenedChanged;
            if (m_editor.IsOpened)
            {
                OnEditorOpened();
            }
        }

        private void OnIsOpenedChanged()
        {
            if (m_editor.IsOpened)
            {
                OnEditorOpened();
            }
            else
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
                OnEditorClosed();
            }
        }

        protected virtual void OnEditorCreated(object obj)
        {
            OnEditorExist();
        }

        protected virtual void OnEditorOpened()
        {

        }

        protected virtual void OnEditorClosed()
        {

        }

        protected void RunNextFrame(Action action)
        {
            StartCoroutine(CoWaitForEndOfFrame(action));
        }

        private IEnumerator CoWaitForEndOfFrame(Action action)
        {
            yield return new WaitForEndOfFrame();
            action();
        }
    }
}

