using Battlehub.RTCommon;
using Battlehub.RTSL.Battlehub.SL2;
using Battlehub.RTSL.Interface;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Battlehub.SL2;
using UnityEngine.SceneManagement;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSL
{
    public static class Menu
    {
        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        [MenuItem("Tools/Runtime SaveLoad/Open Scene")]
        public static void OpenScene()
        {
            if(Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Unable to open scene", "Unable to open scene in play mode", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel("Open Scene", Application.persistentDataPath, "rtscene");
            if (path.Length != 0)
            {
                GameObject projGo = new GameObject();
                IAssetBundleLoader bundleLoader;
                if (File.Exists(Application.streamingAssetsPath + "/credentials.json"))
                {
                    bundleLoader = new GoogleDriveAssetBundleLoader();
                }
                else
                {
                    bundleLoader = new AssetBundleLoader();
                }
                
                IOC.Register(bundleLoader);

                ITypeMap typeMap = new TypeMap();
                IOC.Register(typeMap);

                IUnityObjectFactory objFactory = new UnityObjectFactory();
                IOC.Register(objFactory);
                
                ISerializer serializer = new ProtobufSerializer();
                IOC.Register(serializer);

                IStorage storage = new FileSystemStorage();
                IOC.Register(storage);

                IRuntimeShaderUtil shaderUtil = new RuntimeShaderUtil();  
                IOC.Register(shaderUtil);

                IAssetDB assetDB = new AssetDB();
                IOC.Register<IIDMap>(assetDB);
                IOC.Register(assetDB);

                Project project = projGo.AddComponent<Project>();
                project.Awake_Internal();

                DirectoryInfo root = new DirectoryInfo(Application.persistentDataPath);
                string rootPath = root.ToString().ToLower();

                DirectoryInfo parent = Directory.GetParent(path);
                while (true)
                {
                    if (parent == null)
                    {
                        EditorUtility.DisplayDialog("Unable to open scene", "Project.rtmeta was not found", "OK");
                        UnityObject.DestroyImmediate(projGo);
                        IOC.ClearAll();
                        return;
                    }

                    string projectPath = parent.FullName.ToLower();
                    if (rootPath == projectPath)
                    {
                        EditorUtility.DisplayDialog("Unable to open scene", "Project.rtmeta was not found", "OK");
                        UnityObject.DestroyImmediate(projGo);
                        IOC.ClearAll();
                        return;
                    }

                    string projectFile = Path.Combine(projectPath, "Project.rtmeta");
                    if (File.Exists(projectFile))
                    {
                        string projectFileName = Path.GetFileNameWithoutExtension(projectPath);

                        project.OpenProject(projectFileName, (error, result) =>
                        {
                            if (error.HasError)
                            {
                                EditorUtility.DisplayDialog("Unable to open scene", "Project " + projectFileName + " can not be loaded", "OK");
                                UnityObject.DestroyImmediate(projGo);
                                IOC.ClearAll();
                                return;
                            }

                            string relativePath = GetRelativePath(path, projectPath);
                            relativePath = relativePath.Replace('\\', '/');
                            AssetItem scene = (AssetItem)project.Root.Get(relativePath);
                            project.Load(new[] { scene }, (loadError, loadedObjects) =>
                            {
                                IOC.ClearAll();
                                if (loadError.HasError)
                                {
                                    EditorUtility.DisplayDialog("Unable to open scene", loadError.ToString(), "OK");
                                    UnityObject.DestroyImmediate(projGo);
                                    return;
                                }
                            });
                        });

                        return;
                    }

                    parent = parent.Parent;
                }
            }
        }

        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Create")]
        private static void CreatePersistentClasses()
        {
            PersistentClassMapperWindow.CreatePersistentClasses();
        }

        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Edit")]
        public static void EditPersistentClasses()
        {
            PersistentClassMapperWindow.ShowWindow();
        }

        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Clean")]
        public static void CleanPersistentClasses()
        {
            if(EditorUtility.DisplayDialog("Clean", "Do you want to remove persistent classes and type model?", "Yes", "No"))
            {
                if(EditorUtility.DisplayDialog("Clean", "Do you want to remove files from " + "Assets" + RTSLPath.UserRoot + "/CustomImplementation ?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/CustomImplementation");
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/Mappings/Editor/FilePathStorage.prefab");
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/Scripts");
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/RTSLTypeModel.dll");
                }
                else
                {
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/Scripts");
                    AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/RTSLTypeModel.dll");
                }
                //AssetDatabase.DeleteAsset("Assets" + RTSLPath.UserRoot + "/Mappings");
            }
        }

        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Build Type Model")]
        private static void BuildTypeModel()
        {
            RuntimeTypeModel model = TypeModelCreator.Create();
            string dllName = RTSLPath.TypeModelDll;

            model.Compile(new RuntimeTypeModel.CompilerOptions() { OutputPath = dllName, TypeName = "RTSLTypeModel" });

            string srcPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets")) + dllName;
            string dstPath = Application.dataPath + RTSLPath.UserRoot + "/" + dllName;
            Debug.LogFormat("Done! Move {0} to {1} ...", srcPath, dstPath);
            File.Delete(dstPath);
            File.Move(srcPath, dstPath);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            PluginImporter importer = AssetImporter.GetAtPath("Assets" + RTSLPath.UserRoot + "/" + dllName) as PluginImporter;
            importer.SetCompatibleWithAnyPlatform(true);
            importer.SetExcludeEditorFromAnyPlatform(true);
            importer.SaveAndReimport();
        }

        [MenuItem("Tools/Runtime SaveLoad/Libraries/Collect Scene Dependencies")]
        private static void CreateAssetLibraryForActiveScene()
        {
            CreateBuiltInAssetLibrary();

            Scene scene = SceneManager.GetActiveScene();
            if (scene == null || string.IsNullOrEmpty(scene.name))
            {
                Debug.Log("Unable to create AssetLibrary for scene with no name");
                return;
            }

            int index;
            AssetLibraryAsset asset;
            AssetFolderInfo folder;
            HashSet<UnityObject> hs = ReadFromBuiltInAssetLibraries(out index, out asset, out folder);
            HashSet<UnityObject> hs2 = ReadFromSceneAssetLibraries(scene, out index, out asset, out folder);

            foreach(UnityObject obj in hs)
            {
                if(!hs2.Contains(obj))
                {
                    hs2.Add(obj);
                }
            }

            CreateAssetLibraryForScene(scene, index, asset, folder, hs2);
        }


        [MenuItem("Tools/Runtime SaveLoad/Libraries/Update Built-In Assets Library")]
        private static void CreateBuiltInAssetLibrary()
        {
            int index;
            AssetLibraryAsset asset;
            AssetFolderInfo folder;
            HashSet<UnityObject> hs = ReadFromBuiltInAssetLibraries(out index, out asset, out folder);
            CreateBuiltInAssetLibrary(index, asset, folder, hs);
        }

        [MenuItem("Tools/Runtime SaveLoad/Libraries/Update Shader Profiles")]
        private static void CreateShaderProfiles()
        {
            RuntimeShaderProfilesGen.CreateProfile();
        }

        [MenuItem("Tools/Runtime SaveLoad/Libraries/Update Asset Libraries List")]
        private static void CreateAssetLibrariesList()
        {
            AssetLibrariesListAsset asset = AssetLibrariesListGen.UpdateList();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Assets/Create/Runtime Asset Library", priority = 0)]
        private static void CreateAssetLibrary()
        {
            AssetLibraryAsset asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();

            int identity = AssetLibrariesListGen.GetIdentity();
            asset.Ordinal = identity;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string name = "/AssetLibrary" + ((identity == 0) ? "" : identity.ToString());
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
                        
            Selection.activeObject = asset;

            AssetLibrariesListGen.UpdateList(identity + 1);
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if(EditorPrefs.GetBool("RTSLBuildAll"))
            {
                EditorPrefs.SetBool("RTSLBuildAll", false);

                try
                {
                    CreateAssetLibraryForActiveScene();
                    Debug.Log("Asset Libraries Updated");

                    CreateAssetLibrariesList();
                    Debug.Log("Asset Libraries List Updated");

                    CreateShaderProfiles();
                    Debug.Log("Shader Profiles Updated");

                    EditorUtility.DisplayProgressBar("Build All", "Building Type Model...", 0.66f);
                    BuildTypeModel();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        [MenuItem("Tools/Runtime SaveLoad/Build All")]
        public static void BuildAll()
        {
            EditorUtility.DisplayProgressBar("Build All", "Creating persistent classes", 0.0f);
            try
            {
                CreatePersistentClasses();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log("Persistent Classes Created");

                Selection.activeObject = AssetDatabase.LoadAssetAtPath("Assets" + RTSLPath.UserRoot + "/" + RTSLPath.ScriptsAutoFolder, typeof(UnityObject));
                EditorGUIUtility.PingObject(Selection.activeObject);

                EditorUtility.DisplayProgressBar("Build All", "Updating asset libraries and shader profiles", 0.33f);
                EditorPrefs.SetBool("RTSLBuildAll", true);
            }
            catch
            {
                EditorUtility.ClearProgressBar();
            }   
        }

        private static HashSet<UnityObject> ReadFromAssetLibraries(string[] guids, out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            HashSet<UnityObject> hs = new HashSet<UnityObject>();

            List<AssetLibraryAsset> assetLibraries = new List<AssetLibraryAsset>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                AssetLibraryAsset assetLibrary = AssetDatabase.LoadAssetAtPath<AssetLibraryAsset>(path);
                if (assetLibrary != null)
                {
                    assetLibrary.Foreach(assetInfo =>
                    {
                        if (assetInfo.Object != null)
                        {
                            if (!hs.Contains(assetInfo.Object))
                            {
                                hs.Add(assetInfo.Object);
                            }

                            if (assetInfo.PrefabParts != null)
                            {
                                foreach (PrefabPartInfo prefabPart in assetInfo.PrefabParts)
                                {
                                    if (prefabPart.Object != null)
                                    {
                                        if (!hs.Contains(prefabPart.Object))
                                        {
                                            hs.Add(prefabPart.Object);
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    });

                    assetLibraries.Add(assetLibrary);
                }
            }

            if (assetLibraries.Count == 0)
            {
                asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                index = 0;
            }
            else
            {
                asset = assetLibraries.OrderBy(a => a.AssetLibrary.Identity).FirstOrDefault();
                index = assetLibraries.Count - 1;
            }

            folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
            if (folder.Assets == null)
            {
                folder.Assets = new List<AssetInfo>();
            }
            return hs;
        }

        private static void CreateAssetLibrary(object[] objects, string folderName, string assetLibraryName, int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            int identity = asset.AssetLibrary.Identity;

            foreach (object o in objects)
            {
                UnityObject obj = o as UnityObject;
                if (!obj)
                {
                    if(o != null)
                    {
                        Debug.Log(o.GetType() + " is not a UnityEngine.Object");
                    }
                    continue;
                }

                if (hs.Contains(obj))
                {
                    continue;
                }

                if (!AssetDatabase.Contains(obj))
                {
                    continue;
                }

                if (obj is GameObject)
                {
                    GameObject go = (GameObject)obj;
                    AssetInfo assetInfo = new AssetInfo(go.name, 0, identity);
                    assetInfo.Object = go;
                    hs.Add(go);

                    identity++;

                    List<PrefabPartInfo> prefabParts = new List<PrefabPartInfo>();
                    AssetLibraryAssetsGUI.CreatePefabParts(go, ref identity, prefabParts);
                    for(int i = prefabParts.Count - 1; i >= 0; --i)
                    {
                        PrefabPartInfo prefabPart = prefabParts[i];
                        if (hs.Contains(prefabPart.Object))
                        {
                            prefabParts.Remove(prefabPart);
                        }
                        else
                        {
                            hs.Add(prefabPart.Object);
                        }
                    }

                    if (prefabParts.Count >= AssetLibraryInfo.MAX_ASSETS - AssetLibraryInfo.INITIAL_ID)
                    {
                        EditorUtility.DisplayDialog("Unable Create AssetLibrary", string.Format("Max 'Indentity' value reached. 'Identity' ==  {0}", AssetLibraryInfo.MAX_ASSETS), "OK");
                        return;
                    }

                    if (identity >= AssetLibraryInfo.MAX_ASSETS)
                    {
                        SaveAssetLibrary(asset, folderName, assetLibraryName, index);
                        index++;

                        asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                        folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
                        if (folder.Assets == null)
                        {
                            folder.Assets = new List<AssetInfo>();
                        }
                        identity = asset.AssetLibrary.Identity;
                    }

                    assetInfo.PrefabParts = prefabParts;
                    asset.AssetLibrary.Identity = identity;
                    folder.Assets.Add(assetInfo);
                    assetInfo.Folder = folder;
                }
                else
                {
                    AssetInfo assetInfo = new AssetInfo(obj.name, 0, identity);
                    assetInfo.Object = obj;
                    identity++;

                    if (identity >= AssetLibraryInfo.MAX_ASSETS)
                    {
                        SaveAssetLibrary(asset, folderName, assetLibraryName, index);
                        index++;

                        asset = ScriptableObject.CreateInstance<AssetLibraryAsset>();
                        folder = asset.AssetLibrary.Folders.Where(f => f.depth == 0).First();
                        if (folder.Assets == null)
                        {
                            folder.Assets = new List<AssetInfo>();
                        }
                        identity = asset.AssetLibrary.Identity;
                    }

                    asset.AssetLibrary.Identity = identity;
                    folder.Assets.Add(assetInfo);
                    assetInfo.Folder = folder;
                }
            }

            SaveAssetLibrary(asset, folderName, assetLibraryName, index);
            index++;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void SaveAssetLibrary(AssetLibraryAsset asset, string folderName, string assetLibraryName, int index)
        {
            string dir = RTSLPath.UserRoot;
            string dataPath = Application.dataPath;

            if (!Directory.Exists(dataPath + dir))
            {
                Directory.CreateDirectory(dataPath + dir);
            }

            if (!Directory.Exists(dataPath + dir + "/" + RTSLPath.LibrariesFolder))
            {
                AssetDatabase.CreateFolder("Assets" + dir, RTSLPath.LibrariesFolder);
            }

            dir = dir + "/" + RTSLPath.LibrariesFolder;
            if (!Directory.Exists(dataPath + dir + "/Resources"))
            {
                AssetDatabase.CreateFolder("Assets" + dir, "Resources");
            }

            dir = dir + "/Resources";

            string[] folderNameParts = folderName.Split('/');
            for (int i = 0; i < folderNameParts.Length; ++i)
            {
                string folderNamePart = folderNameParts[i];

                if (!Directory.Exists(dataPath + dir + "/" + folderNamePart))
                {
                    AssetDatabase.CreateFolder("Assets" + dir, folderNamePart);
                }

                dir = dir + "/" + folderNamePart;
            }

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                if (index == 0)
                {
                    AssetDatabase.CreateAsset(asset, "Assets" + dir + "/" + assetLibraryName + ".asset");
                }
                else
                {
                    AssetDatabase.CreateAsset(asset, "Assets" + dir + "/" + assetLibraryName + (index + 1) + ".asset");
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static HashSet<UnityObject> ReadFromSceneAssetLibraries(Scene scene, out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            if (!Directory.Exists(Application.dataPath + RTSLPath.UserRoot + "/" + RTSLPath.LibrariesFolder + "/Resources/Scenes/" + scene.name))
            {
                return ReadFromAssetLibraries(new string[0], out index, out asset, out folder);
            }
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" + RTSLPath.UserRoot + "/" + RTSLPath.LibrariesFolder + "/Resources/Scenes/" + scene.name });
            return ReadFromAssetLibraries(guids, out index, out asset, out folder);
        }

        private static void CreateAssetLibraryForScene(Scene scene, int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            TypeMap typeMap = new TypeMap();
            AssetDB assetDB = new AssetDB();
            RuntimeShaderUtil shaderUtil = new RuntimeShaderUtil();

            IOC.Register<ITypeMap>(typeMap);
            IOC.Register<IAssetDB>(assetDB);
            IOC.Register<IRuntimeShaderUtil>(shaderUtil);

            PersistentRuntimeScene rtScene = new PersistentRuntimeScene();

            GetDepsFromContext ctx = new GetDepsFromContext();
            rtScene.GetDepsFrom(scene, ctx);

            Queue<UnityObject> depsQueue = new Queue<UnityObject>(ctx.Dependencies.OfType<UnityObject>());
            GetDepsFromContext getDepsCtx = new GetDepsFromContext();
            while (depsQueue.Count > 0)
            {
                UnityObject uo = depsQueue.Dequeue();
                if (!uo)
                {
                    continue;
                }

                Type persistentType = typeMap.ToPersistentType(uo.GetType());
                if (persistentType != null)
                {
                    getDepsCtx.Clear();

                    try
                    {
                        PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
                        //persistentObject.ReadFrom(uo);
                        persistentObject.GetDepsFrom(uo, getDepsCtx);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    foreach (UnityObject dep in getDepsCtx.Dependencies)
                    {
                        if (!ctx.Dependencies.Contains(dep))
                        {
                            ctx.Dependencies.Add(dep);
                            depsQueue.Enqueue(dep);
                        }
                    }
                }
            }

            IOC.Unregister<IRuntimeShaderUtil>(shaderUtil);
            IOC.Unregister<ITypeMap>(typeMap);
            IOC.Unregister<IAssetDB>(assetDB);

            CreateAssetLibrary(ctx.Dependencies.ToArray(), "Scenes/" + scene.name, "SceneAssetLibrary", index, asset, folder, hs);
        }

        private static HashSet<UnityObject> ReadFromBuiltInAssetLibraries(out int index, out AssetLibraryAsset asset, out AssetFolderInfo folder)
        {
            if (!Directory.Exists(Application.dataPath + RTSLPath.UserRoot + "/" + RTSLPath.LibrariesFolder + "/Resources/BuiltInAssets"))
            {
                return ReadFromAssetLibraries(new string[0], out index, out asset, out folder);
            }
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" + RTSLPath.UserRoot + "/" + RTSLPath.LibrariesFolder + "/Resources/BuiltInAssets" });
            return ReadFromAssetLibraries(guids, out index, out asset, out folder);
        }

        private static void CreateBuiltInAssetLibrary(int index, AssetLibraryAsset asset, AssetFolderInfo folder, HashSet<UnityObject> hs)
        {
            Dictionary<string, Type> builtInExtra = new Dictionary<string, Type>
            {
                {  "Default-Line.mat", typeof(Material) },
                {  "Default-Material.mat", typeof(Material) },
                {  "Default-Particle.mat", typeof(Material) },
                {  "Default-Skybox.mat", typeof(Material) },
                {  "Sprites-Default.mat", typeof(Material) },
                {  "Sprites-Mask.mat", typeof(Material) },
                {  "UI/Skin/Background.psd", typeof(Sprite) },
                {  "UI/Skin/Checkmark.psd", typeof(Sprite) },
                {  "UI/Skin/DropdownArrow.psd", typeof(Sprite) },
                {  "UI/Skin/InputFieldBackground.psd", typeof(Sprite) },
                {  "UI/Skin/Knob.psd", typeof(Sprite) },
                {  "UI/Skin/UIMask.psd", typeof(Sprite) },
                {  "UI/Skin/UISprite.psd", typeof(Sprite) },
            };

            Dictionary<string, Type> builtIn = new Dictionary<string, Type>
            {
               { "New-Sphere.fbx", typeof(Mesh) },
               { "New-Capsule.fbx", typeof(Mesh) },
               { "New-Cylinder.fbx", typeof(Mesh) },
               { "Cube.fbx", typeof(Mesh) },
               { "New-Plane.fbx", typeof(Mesh) },
               { "Quad.fbx", typeof(Mesh) },
               { "Arial.ttf", typeof(Font) }
            };

            List<object> builtInAssets = new List<object>();
            foreach (KeyValuePair<string, Type> kvp in builtInExtra)
            {
                UnityObject obj = AssetDatabase.GetBuiltinExtraResource(kvp.Value, kvp.Key);
                if (obj != null)
                {
                    builtInAssets.Add(obj);
                }
            }

            foreach (KeyValuePair<string, Type> kvp in builtIn)
            {
                UnityObject obj = Resources.GetBuiltinResource(kvp.Value, kvp.Key);
                if (obj != null)
                {
                    builtInAssets.Add(obj);
                }
            }
            CreateAssetLibrary(builtInAssets.ToArray(), "BuiltInAssets", "BuiltInAssetLibrary", index, asset, folder, hs);
        }
    }

}

