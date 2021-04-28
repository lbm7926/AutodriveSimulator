using Battlehub.RTCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSL.Interface
{
    public delegate void ProjectEventHandler(Error error);
    public delegate void ProjectEventHandler<T>(Error error, T result);
    public delegate void ProjectEventHandler<T, T2>(Error error, T result, T2 result2);

    public interface IProject
    {
        event ProjectEventHandler NewSceneCreating;
        event ProjectEventHandler NewSceneCreated;
        event ProjectEventHandler<ProjectInfo> CreateProjectCompleted;
        event ProjectEventHandler<ProjectInfo> OpenProjectCompleted;
        event ProjectEventHandler<string> DeleteProjectCompleted;
        event ProjectEventHandler<ProjectInfo[]> ListProjectsCompleted;
        event ProjectEventHandler CloseProjectCompleted;

        event ProjectEventHandler<ProjectItem[]> GetAssetItemsCompleted;
        event ProjectEventHandler<object[]> BeginSave;
        event ProjectEventHandler<AssetItem[], bool> SaveCompleted;
        event ProjectEventHandler<AssetItem[]> BeginLoad;
        event ProjectEventHandler<AssetItem[], UnityObject[]> LoadCompleted;
        event ProjectEventHandler<AssetItem[]> DuplicateCompleted;
        event ProjectEventHandler UnloadCompleted;
        event ProjectEventHandler<AssetItem[]> ImportCompleted;
        event ProjectEventHandler<ProjectItem[]> BeforeDeleteCompleted;
        event ProjectEventHandler<ProjectItem[]> DeleteCompleted;
        event ProjectEventHandler<ProjectItem[], ProjectItem[]> MoveCompleted;
        event ProjectEventHandler<ProjectItem> RenameCompleted;
        event ProjectEventHandler<ProjectItem> CreateCompleted;

        bool IsBusy
        {
            get;
        }

        bool IsOpened
        {
            get;
        }

        ProjectInfo ProjectInfo
        {
            get;
        }

        ProjectItem Root
        {
            get;
        }

        AssetItem LoadedScene
        {
            get;
            set;
        }

        bool IsStatic(ProjectItem projectItem);
        bool IsScene(ProjectItem projectItem);
        Type ToType(AssetItem assetItem);
        Guid ToGuid(Type type);
        long ToID(UnityObject obj);
        T FromID<T>(long id) where T : UnityObject;
        AssetItem ToAssetItem(UnityObject obj);
        AssetItem[] GetDependantAssetItems(AssetItem[] assetItems);
        
        string GetExt(object obj);
        string GetExt(Type type);
        string GetUniqueName(string name, string[] names);
        string GetUniqueName(string name, Type type, ProjectItem folder);
        string GetUniquePath(string path, Type type, ProjectItem folder);

        void CreateNewScene();
        ProjectAsyncOperation<ProjectInfo> CreateProject(string project, ProjectEventHandler<ProjectInfo> callback = null);
        ProjectAsyncOperation<ProjectInfo> OpenProject(string project, ProjectEventHandler<ProjectInfo> callback = null);
        ProjectAsyncOperation<ProjectInfo[]> GetProjects(ProjectEventHandler<ProjectInfo[]> callback = null);
        ProjectAsyncOperation<string> DeleteProject(string project, ProjectEventHandler<string> callback = null);
        void CloseProject();

        ProjectAsyncOperation<AssetItem[]> GetAssetItems(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null); /*no events raised*/
        ProjectAsyncOperation<ProjectItem[]> GetAssetItems(ProjectItem[] folders, ProjectEventHandler<ProjectItem[]> callback = null); /*GetAssetItemsCompleted raised*/

        ProjectAsyncOperation<object[]> GetDependencies(object obj, bool exceptMappedObject = false, ProjectEventHandler<object[]> callback = null); /*no events raised*/

        ProjectAsyncOperation<AssetItem[]> Save(AssetItem[] assetItems, object[] obj, ProjectEventHandler<AssetItem[]> callback = null);
        ProjectAsyncOperation<AssetItem[]> Save(ProjectItem[] parents, byte[][] previewData, object[] obj, string[] nameOverrides, ProjectEventHandler<AssetItem[]> callback = null);
        ProjectAsyncOperation<AssetItem[]> SavePreview(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null);
        ProjectAsyncOperation<AssetItem[]> Duplicate(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null);

        ProjectAsyncOperation<UnityObject[]> Load(AssetItem[] assetItems, ProjectEventHandler<UnityObject[]> callback = null);
        ProjectAsyncOperation Unload(ProjectEventHandler completedCallback = null);
        void Unload(AssetItem[] assetItems);

        ProjectAsyncOperation<ProjectItem> LoadImportItems(string path, bool isBuiltIn, ProjectEventHandler<ProjectItem> callback = null);
        void UnloadImportItems(ProjectItem importItemsRoot);
        ProjectAsyncOperation<AssetItem[]> Import(ImportItem[] importItems, ProjectEventHandler<AssetItem[]> callback = null);

        ProjectAsyncOperation<ProjectItem> CreateFolder(ProjectItem projectItem, ProjectEventHandler<ProjectItem> callback = null);
        ProjectAsyncOperation<ProjectItem> Rename(ProjectItem projectItem, string oldName, ProjectEventHandler<ProjectItem> callback = null);
        ProjectAsyncOperation<ProjectItem[], ProjectItem[]> Move(ProjectItem[] projectItems, ProjectItem target, ProjectEventHandler<ProjectItem[], ProjectItem[]> callback = null);
        ProjectAsyncOperation<ProjectItem[]> Delete(ProjectItem[] projectItems, ProjectEventHandler<ProjectItem[]> callback = null);

        ProjectAsyncOperation<string[]> GetAssetBundles(ProjectEventHandler<string[]> callback = null);
        Dictionary<int, string> GetStaticAssetLibraries();

        ProjectAsyncOperation<T[]> GetValues<T>(string searchPattern, ProjectEventHandler<T[]> callback = null) where T : new();
        ProjectAsyncOperation<T> GetValue<T>(string key, ProjectEventHandler<T> callback = null) where T : new();
        ProjectAsyncOperation SetValue<T>(string key, T obj, ProjectEventHandler callback = null);
        ProjectAsyncOperation DeleteValue<T>(string key, ProjectEventHandler callback = null);
    }

    public class ProjectAsyncOperation : CustomYieldInstruction
    {
        public bool HasError
        {
            get { return Error.HasError; }
        }

        public Error Error
        {
            get;
            set;
        }
        public bool IsCompleted
        {
            get;
            set;
        }
        public override bool keepWaiting
        {
            get { return !IsCompleted; }
        }
    }

    public class ProjectAsyncOperation<T> : ProjectAsyncOperation
    {
        public T Result
        {
            get;
            set;
        }
    }

    public class ProjectAsyncOperation<T, T2> : ProjectAsyncOperation<T>
    {
        public T2 Result2
        {
            get;
            set;
        }
    }

    public static class IProjectExtensions
    {
        public static string GetUniqueName(this IProject project, string path, Type type)
        {
            ProjectItem folder = project.GetFolder(Path.GetDirectoryName(path));
            return Path.GetFileName(project.GetUniquePath(path, type, folder));
        }

        public static string GetUniquePath(this IProject project, string path, Type type)
        {
            ProjectItem folder = project.GetFolder(Path.GetDirectoryName(path));
            return project.GetUniquePath(path, type, folder);
        }

        public static string[] Find<T>(this IProject project, string filter = null, bool allowSubclasses = false)
        {
            Type typeofT = typeof(T);
            return Find(project, filter, allowSubclasses, typeofT);
        }

        public static string[] Find(this IProject project, string filter, bool allowSubclasses, Type typeofT)
        {
            List<string> result = new List<string>();
            ProjectItem[] projectItems = project.Root.Flatten(true);
            for (int i = 0; i < projectItems.Length; ++i)
            {
                AssetItem assetItem = (AssetItem)projectItems[i];
                Type type = project.ToType(assetItem);
                if (type == null)
                {
                    continue;
                }

                if (type != typeofT)
                {
                    if (!allowSubclasses || !type.IsSubclassOf(typeofT))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(filter) && !assetItem.Name.Contains(filter))
                {
                    continue;
                }

                result.Add(assetItem.RelativePath(allowSubclasses));
            }
            return result.ToArray();
        }

        public static string[] FindFolders(this IProject project, string filter = null)
        {
            List<string> result = new List<string>();
            ProjectItem[] projectItems = project.Root.Flatten(false, true);
            for (int i = 0; i < projectItems.Length; ++i)
            {
                ProjectItem projectItem = projectItems[i];
                Debug.Assert(projectItem.IsFolder);

                if (!string.IsNullOrEmpty(filter) && !projectItem.Name.Contains(filter))
                {
                    continue;
                }

                result.Add(projectItem.RelativePath(false));
            }
            return result.ToArray();
        }

        public static ProjectItem Get<T>(this IProject project, string path)
        {
            Type type = typeof(T);
            return Get(project, path, type);
        }
        
        public static ProjectItem Get(this IProject project, string path, Type type)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            return project.Root.Get(string.Format("{0}/{1}{2}", project.Root.Name, path, project.GetExt(type)));
        }

        public static ProjectItem GetFolder(this IProject project, string path = null, bool forceCreate = false)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            if(string.IsNullOrEmpty(path))
            {
                return project.Root;
            }

            return project.Root.Get(string.Format("{0}/{1}", project.Root.Name, path), forceCreate);
        }

        public static bool FolderExist(this IProject project, string path)
        {
            ProjectItem projectItem = project.GetFolder(path);
            return projectItem != null && projectItem.ToString().ToLower() == ("/Assets/" + path).ToLower();
        }

        public static bool Exist<T>(this IProject project, string path)
        {
            ProjectItem projectItem = project.Get<T>(path);
            return projectItem != null && projectItem.ToString().ToLower() == ("/Assets/" + path + projectItem.Ext).ToLower();
        }

        public static ProjectAsyncOperation CreateFolder(this IProject project, string path)
        {
            ProjectItem folder = project.Root.Get(string.Format("{0}/{1}", project.Root.Name, path), true);
            return project.CreateFolder(folder);
        }

        public static ProjectAsyncOperation DeleteFolder(this IProject project, string path)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            path = string.Format("{0}/{1}", project.Root.Name, path);
            ProjectItem projectItem = project.Root.Get(path) as ProjectItem;
            if (projectItem == null)
            {
                throw new ArgumentException("not found", "path");
            }

            return project.Delete(new[] { projectItem });
        }

        public static ProjectAsyncOperation CreatePrefab(this IProject project, string folderPath, GameObject prefab, bool includeDeps, Func<UnityObject, byte[]> createPreview = null)
        {
            ProjectAsyncOperation ao = new ProjectAsyncOperation();

            folderPath = string.Format("{0}/{1}", project.Root.Name, folderPath);
            ProjectItem folder = project.Root.Get(folderPath, true) as ProjectItem;
            if(folder is AssetItem)
            {
                throw new ArgumentException("folderPath");
            }

            if(includeDeps)
            {
                project.GetDependencies(prefab, true, (error, deps) =>
                {
                    object[] objects;
                    if (!deps.Contains(prefab))
                    {
                        objects = new object[deps.Length + 1];
                        objects[deps.Length] = prefab;
                        for (int i = 0; i < deps.Length; ++i)
                        {
                            objects[i] = deps[i];
                        }
                    }
                    else
                    {
                        objects = deps;
                    }

                    IUnityObjectFactory uoFactory = IOC.Resolve<IUnityObjectFactory>();
                    objects = objects.Where(obj => uoFactory.CanCreateInstance(obj.GetType())).ToArray();

                    byte[][] previewData = new byte[objects.Length][];
                    if (createPreview != null)
                    {
                        for (int i = 0; i < objects.Length; ++i)
                        {
                            if (objects[i] is UnityObject)
                            {
                                previewData[i] = createPreview((UnityObject)objects[i]);
                            }
                        }
                    }

                    project.Save(new[] { folder }, previewData, objects, null, (saveErr, assetItems) =>
                    {
                        ao.Error = saveErr;
                        ao.IsCompleted = true;
                    });
                });
            }
            else
            {
                byte[][] previewData = new byte[1][];
                if (createPreview != null)
                {
                    previewData[0] = createPreview(prefab);
                }

                project.Save(new[] { folder }, previewData, new[] { prefab }, null, (saveErr, assetItems) =>
                {
                    ao.Error = saveErr;
                    ao.IsCompleted = true;
                });
            }

            return ao;
        }

        public static ProjectAsyncOperation<AssetItem[]> Save(this IProject project, string path, object obj, byte[] preview = null)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            string name = Path.GetFileName(path);
            path = Path.GetDirectoryName(path).Replace(@"\", "/");
            path = !string.IsNullOrEmpty(path) ? string.Format("{0}/{1}", project.Root.Name, path) : project.Root.Name;

            string ext = project.GetExt(obj.GetType());
            ProjectItem item = project.Root.Get(path + "/" + name + ext);
            if (item is AssetItem)
            {
                AssetItem assetItem = (AssetItem)item;
                return project.Save(new[] { assetItem }, new[] { obj });
            }

            ProjectItem folder = project.Root.Get(path);
            if (folder == null || !folder.IsFolder)
            {
                throw new ArgumentException("directory cannot be found", "path");
            }

            if(preview == null)
            {
                preview = new byte[0];
            }

            return project.Save(new[] { folder }, new[] { preview }, new[] { obj }, new[] { name });
        }

        public static ProjectAsyncOperation<UnityObject[]> Load<T>(this IProject project, string path)
        {
            Type type = typeof(T);
            return Load(project, path, type);
        }

        public static ProjectAsyncOperation<UnityObject[]> Load(this IProject project, string path, Type type)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            path = string.Format("{0}/{1}", project.Root.Name, path);

            AssetItem assetItem = project.Root.Get(path + project.GetExt(type)) as AssetItem;
            if (assetItem == null)
            {
                throw new ArgumentException("not found", "path");
            }

            return project.Load(new[] { assetItem });
        }

        public static ProjectAsyncOperation Delete<T>(this IProject project, string path)
        {
            Type type = typeof(T);
            return Delete(project, path, type);
        }

        public static ProjectAsyncOperation Delete(this IProject project, string path, Type type)
        {
            if (!project.IsOpened)
            {
                throw new InvalidOperationException("OpenProject first");
            }

            path = string.Format("{0}/{1}", project.Root.Name, path);

            AssetItem projectItem = project.Root.Get(path + project.GetExt(type)) as AssetItem;
            if (projectItem == null)
            {
                throw new ArgumentException("not found", "path");
            }

            return project.Delete(new[] { projectItem });
        }

        public static void Unload<T>(this IProject project, string path)
        {
            project.Unload(path, typeof(T));
        }

        public static void Unload(this IProject project, string path, Type type)
        {
            AssetItem unloadItem = (AssetItem)project.Get(path, type);
            if(unloadItem == null)
            {
                Debug.Log("Unable to unload. Item was not found " + path);
                return;
            }
            project.Unload(new[] { unloadItem });
        }
    }
}
