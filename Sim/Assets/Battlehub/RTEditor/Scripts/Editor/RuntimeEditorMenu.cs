using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public static class RTEditorMenu
    {
        const string root = BHPath.Root + @"/RTEditor/";

        [MenuItem("Tools/Runtime Editor/Create")]
        public static void CreateRuntimeEditor()
        {
            Undo.RegisterCreatedObjectUndo(InstantiateRuntimeEditor(), "Battlehub.RTEditor.Create");
            if (!UnityObject.FindObjectOfType<EventSystem>())
            {
                GameObject es = new GameObject();
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                es.name = "EventSystem";
            }
        }

        public static GameObject InstantiateRuntimeEditor()
        {
            return InstantiatePrefab("RuntimeEditor.prefab");
        }

        public static GameObject InstantiatePrefab(string name)
        {
            UnityObject prefab = AssetDatabase.LoadAssetAtPath("Assets/" + root + "Prefabs/" + name, typeof(GameObject));
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

    }

}
