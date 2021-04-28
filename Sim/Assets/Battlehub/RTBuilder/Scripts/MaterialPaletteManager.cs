using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTSL;
using Battlehub.RTSL.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTBuilder
{
    public interface IMaterialPaletteManager
    {
        event Action<Material> MaterialCreated;
        event Action<Material> MaterialAdded;
        event Action<Material> MaterialRemoved;
        event Action<MaterialPalette> PaletteChanged;

        bool IsReady
        {
            get;
        }

        MaterialPalette Palette
        {
            get;
        }
        
        void CreateMaterial();
        void AddMaterial(Material material, bool setUniqueName = false);
        void RemoveMaterial(Material material);
    }

    [DefaultExecutionOrder(-50)]
    public class MaterialPaletteManager : MonoBehaviour, IMaterialPaletteManager
    {
        public event Action<Material> MaterialCreated;
        public event Action<Material> MaterialAdded;
        public event Action<Material> MaterialRemoved;
        public event Action<MaterialPalette> PaletteChanged;

        public bool IsReady
        {
            get;
            private set;
        }

        private MaterialPalette m_palette;
        public MaterialPalette Palette
        {
            get { return m_palette; }
        }

        private IRuntimeEditor m_editor;
        private IProBuilderTool m_proBuilderTool;

        private void Awake()
        {
            IOC.RegisterFallback<IMaterialPaletteManager>(this);

            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.SceneLoaded += OnSceneLoaded;

            m_proBuilderTool = IOC.Resolve<IProBuilderTool>();

            InitPalette();
            IsReady = true;
        }

        private void OnDestroy()
        {
            if(m_editor != null)
            {
                m_editor.SceneLoaded -= OnSceneLoaded;
            }
            IOC.UnregisterFallback<IMaterialPaletteManager>(this);
        }

        private void OnSceneLoaded()
        {
            InitPalette();
            if(PaletteChanged != null)
            {
                PaletteChanged(m_palette);
            }
        }

        private void InitPalette()
        {
            m_palette = FindObjectOfType<MaterialPalette>();
            if (m_palette == null)
            {
                GameObject go = new GameObject("MaterialPalette");
                m_palette = go.AddComponent<MaterialPalette>();

                Material material = Instantiate(PBBuiltinMaterials.DefaultMaterial);
                material.name = "Default";
                m_palette.Materials.Add(material);
            }
            CleanPalette();
        }

        public void CreateMaterial()
        {
            Material material = Instantiate(PBBuiltinMaterials.DefaultMaterial);
            material.name = PathHelper.GetUniqueName("Material", m_palette.Materials.Select(m => m.name).ToList());
            m_palette.Materials.Add(material);

            if(MaterialCreated != null)
            {
                MaterialCreated(material);
            }
        }

        public void AddMaterial(Material material, bool setUniqueName)
        {
            if(setUniqueName)
            {
                material.name = PathHelper.GetUniqueName("Material", m_palette.Materials.Select(m => m.name).ToList());
            }

            m_palette.Materials.Add(material);
            if(MaterialAdded != null)
            {
                MaterialAdded(material);
            }
        }

        public void RemoveMaterial(Material material)
        {
            m_palette.Materials.Remove(material);
            if(MaterialRemoved != null)
            {
                MaterialRemoved(material);
            }
        }

        private void CleanPalette()
        {
            if (m_palette.Materials == null)
            {
                m_palette.Materials = new List<Material>();
                return;
            }

            m_palette.Materials = m_palette.Materials.Where(m => m != null).ToList();
        }


        protected virtual void Update()
        {
            if (m_editor.ActiveWindow == null || m_editor.ActiveWindow != this && m_editor.ActiveWindow.WindowType != RuntimeWindowType.Scene)
            {
                return;
            }

            IInput input = m_editor.Input;
            bool select = input.GetKey(KeyCode.S);
            bool unselect = input.GetKey(KeyCode.U);

            if (!select && !unselect)
            {
                if (!m_proBuilderTool.HasSelection)
                {
                    return;
                }
            }
            
            if (!input.GetKey(KeyCode.LeftAlt) && !input.GetKey(KeyCode.RightAlt) && !input.GetKey(KeyCode.AltGr))
            {
                return;
            }

            for (int keyCode = (int)KeyCode.Alpha0; keyCode <= (int)KeyCode.Alpha0 + 9; ++keyCode)
            {
                if (!input.GetKeyDown((KeyCode)keyCode))
                {
                    continue;
                }

                int index = keyCode - (int)KeyCode.Alpha1;
                if (index == -1)
                {
                    index = 9;
                }

                if (0 <= index && index < Palette.Materials.Count)
                {
                    Material material = Palette.Materials[index];
                    if (material == null)
                    {
                        break;
                    }

                    if(select)
                    {
                        m_proBuilderTool.SelectFaces(material);
                    }
                    else if(unselect)
                    {
                        m_proBuilderTool.UnselectFaces(material);
                    }
                    else
                    {
                        m_proBuilderTool.ApplyMaterial(material);
                    }
                }
            }
        }

    }

        /* Persistent MaterialPaletteManager */
        /*
        [DefaultExecutionOrder(-50)]
        public class MaterialPaletteManager : MonoBehaviour, IMaterialPaletteManager
        {
            public event Action<Material> MaterialCreated;
            public event Action<Material> MaterialAdded;
            public event Action<Material> MaterialRemoved;

            public bool IsReady
            {
                get;
                private set;
            }

            private MaterialPalette m_palette;
            public MaterialPalette Palette
            {
                get { return m_palette; }
            }

            private const string DataFolder = "RTBuilderData/";
            private const string PaletteFile = "DefaultMaterialPalette";
            private const string MaterialFile = "Material";

            private IProject m_project;
            private IRTE m_editor;

            private void Awake()
            {
                IOC.RegisterFallback<IMaterialPaletteManager>(this);
            }

            private void OnDestroy()
            {
                IOC.UnregisterFallback<IMaterialPaletteManager>(this);
            }

            private IEnumerator Start()
            {
                m_editor = IOC.Resolve<IRTE>();
                m_project = IOC.Resolve<IProject>();

                yield return new WaitUntil(() => m_project.IsOpened);
                yield return new WaitWhile(() => m_editor.IsBusy);

                m_editor.IsBusy = true;
                if (!m_project.Exist<MaterialPalette>(DataFolder + PaletteFile))
                {
                    m_palette = ScriptableObject.CreateInstance<MaterialPalette>();
                    m_palette.Materials = new List<Material>();

                    yield return m_project.CreateFolder(DataFolder.Replace("/", ""));
                    ProjectAsyncOperation<Material> saveMaterialAo = SaveMaterial(PBBuiltinMaterials.DefaultMaterial);
                    yield return saveMaterialAo;

                    if (saveMaterialAo.HasError)
                    {
                        m_editor.IsBusy = false;
                        Debug.Log("Unable to save Default Material " + saveMaterialAo.Error);
                        yield break;
                    }

                    m_palette.Materials.Add(saveMaterialAo.Result);
                    ProjectAsyncOperation savePaletteAo = m_project.Save(DataFolder + PaletteFile, m_palette);
                    yield return savePaletteAo;

                    if (savePaletteAo.HasError)
                    {
                        m_editor.IsBusy = false;
                        Debug.Log("Unable to save DefaultMaterialPalette " + savePaletteAo.Error);
                        yield break;
                    }
                }

                ProjectAsyncOperation<UnityObject[]> loadAo = m_project.Load<MaterialPalette>(DataFolder + PaletteFile);
                yield return loadAo;

                if (loadAo.HasError)
                {
                    m_editor.IsBusy = false;
                    Debug.Log("Unable to load DefaultMaterialPalette " + loadAo.Error);
                    yield break;
                }

                m_palette = (MaterialPalette)loadAo.Result[0];
                CleanPalette();

                m_editor.IsBusy = false;
                IsReady = true;
            }

            public void AddMaterial(Material material)
            {
                StartCoroutine(CoAddAndApplyMaterial(material));
            }

            public void CreateMaterial()
            {
                StartCoroutine(CoCreateMaterial());
            }

            public void RemoveMaterial(Material material)
            {
                StartCoroutine(CoRemoveMaterial(material));
            }

            private ProjectAsyncOperation<Material> SaveMaterial(Material material)
            {
                ProjectAsyncOperation<Material> ao = new ProjectAsyncOperation<Material>();
                StartCoroutine(CoSaveMaterial(material, ao));
                return ao;
            }

            private IEnumerator CoSaveMaterial(Material material, ProjectAsyncOperation<Material> ao)
            {
                material.name = m_project.GetUniqueName(DataFolder + MaterialFile, typeof(Material));

                string uniquePath = m_project.GetUniquePath(DataFolder + MaterialFile, typeof(Material));
                ProjectAsyncOperation saveAo = m_project.Save(uniquePath, material);
                yield return saveAo;

                ProjectAsyncOperation<UnityObject[]> loadAo = m_project.Load<Material>(uniquePath);
                yield return loadAo;

                ao.Result = (Material)loadAo.Result[0];
                ao.IsCompleted = true;
                ao.Error = loadAo.Error;
            }

            private IEnumerator CoAddAndApplyMaterial(Material material)
            {
                yield return new WaitWhile(() => m_editor.IsBusy);

                m_editor.IsBusy = true;

                ProjectAsyncOperation<Material> ao = SaveMaterial(material);
                yield return ao;

                Destroy(material);

                m_palette.Materials.Add(ao.Result);

                if (MaterialAdded != null)
                {
                    MaterialAdded(ao.Result);
                }

                m_editor.IsBusy = false;

                StartCoroutine(CoSavePalette());
            }

            private IEnumerator CoCreateMaterial()
            {
                yield return new WaitWhile(() => m_editor.IsBusy);

                m_editor.IsBusy = true;

                ProjectAsyncOperation<Material> ao = SaveMaterial(PBBuiltinMaterials.DefaultMaterial);
                yield return ao;

                m_palette.Materials.Add(ao.Result);

                if (MaterialCreated != null)
                {
                    MaterialCreated(ao.Result);
                }

                m_editor.IsBusy = false;

                StartCoroutine(CoSavePalette());
            }

            private IEnumerator CoRemoveMaterial(Material material)
            {
                yield return new WaitWhile(() => m_editor.IsBusy);

                m_editor.IsBusy = true;
                AssetItem assetItem = m_project.ToAssetItem(material);
                yield return m_project.Delete(new[] { assetItem });

                m_palette.Materials.Remove(material);

                if(MaterialRemoved != null)
                {
                    MaterialRemoved(material);
                }

                m_editor.IsBusy = false;

                StartCoroutine(CoSavePalette());
            }

            private IEnumerator CoSavePalette()
            {
                yield return new WaitWhile(() => m_editor.IsBusy);

                m_editor.IsBusy = true;
                ProjectAsyncOperation saveAo = m_project.Save(DataFolder + PaletteFile, m_palette);
                yield return saveAo;

                m_editor.IsBusy = false;
                if (saveAo.HasError)
                {
                    Debug.Log("Unable to save DefaultMaterialPalette " + saveAo.Error);
                    yield break;
                }
            }

            private void CleanPalette()
            {
                if(m_palette.Materials == null)
                {
                    m_palette.Materials = new List<Material>();
                    return;
                }

                m_palette.Materials = m_palette.Materials.Where(m => m != null).ToList();
            }
        }
        */

    }
