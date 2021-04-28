using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using UnityEngine.Battlehub.SL2;
using Battlehub.RTSL.Interface;
using Battlehub.RTCommon;
using System.Linq;
using System.Threading;
using Battlehub.Utils;

namespace Battlehub.RTSL
{
    public delegate void StorageEventHandler(Error error);
    public delegate void StorageEventHandler<T>(Error error, T data);
    public delegate void StorageEventHandler<T, T2>(Error error, T data, T2 data2);

    public interface IStorage
    {
        void CreateProject(string projectPath, StorageEventHandler<ProjectInfo> callback);
        void DeleteProject(string projectPath, StorageEventHandler callback);
        void GetProjects(StorageEventHandler<ProjectInfo[]> callback);
        void GetProject(string projectPath, StorageEventHandler<ProjectInfo, AssetBundleInfo[]> callback);
        void GetProjectTree(string projectPath, StorageEventHandler<ProjectItem> callback);
        void GetPreviews(string projectPath, string[] assetPath, StorageEventHandler<Preview[]> callback);
        void GetPreviews(string projectPath, string[] folderPath, StorageEventHandler<Preview[][]> callback);
        void Save(string projectPath, string[] folderPaths, AssetItem[] assetItems, PersistentObject[] persistentObjects, ProjectInfo projectInfo, bool previewOnly, StorageEventHandler callback);
        void Save(string projectPath, AssetBundleInfo assetBundleInfo, ProjectInfo project, StorageEventHandler callback);
        void Load(string projectPath, string[] assetPaths, Type[] types, StorageEventHandler<PersistentObject[]> callback);
        void Load(string projectPath, string bundleName, StorageEventHandler<AssetBundleInfo> callback);
        void Delete(string projectPath, string[] paths, StorageEventHandler callback);
        void Move(string projectPath, string[] paths, string[] names, string targetPath, StorageEventHandler callback);
        void Rename(string projectPath, string[] paths, string[] oldNames, string[] names, StorageEventHandler callback);
        void Create(string projectPath, string[] paths, string[] names, StorageEventHandler callback);
        void GetValue(string projectPath, string key, Type type, StorageEventHandler<PersistentObject> callback);
        void GetValues(string projectPath, string searchPattern, Type type, StorageEventHandler<PersistentObject[]> callback);
        void SetValue(string projectPath, string key, PersistentObject persistentObject, StorageEventHandler callback);
        void DeleteValue(string projectPath, string key, StorageEventHandler callback);
    }

    public class FileSystemStorage : IStorage
    {
        private const string MetaExt = ".rtmeta";
        private const string PreviewExt = ".rtview";
        private const string KeyValueStorage = "Values";

        private string RootPath
        {
            get;
            set;
        }

        private string FullPath(string path)
        {
            return RootPath + path;
        }

        private string AssetsFolderPath(string path)
        {
            return RootPath + path + "/Assets";
        }

        public FileSystemStorage()
        {
            RootPath = Application.persistentDataPath + "/";
            Debug.LogFormat("RootPath : {0}", RootPath);
        }

        public void CreateProject(string projectName, StorageEventHandler<ProjectInfo> callback)
        {
            string projectDir = FullPath(projectName);
            if (Directory.Exists(projectDir))
            {
                Error error = new Error(Error.E_AlreadyExist);
                error.ErrorText = "Project with the same name already exists " + projectName;
                callback(error, null);
            }
            else
            {
                ISerializer serializer = IOC.Resolve<ISerializer>();
                Directory.CreateDirectory(projectDir);
                ProjectInfo projectInfo = null;
                using (FileStream fs = File.OpenWrite(projectDir + "/Project.rtmeta"))
                {
                    projectInfo = new ProjectInfo
                    {
                        Name = projectName,
                        LastWriteTime = DateTime.UtcNow
                    };

                    serializer.Serialize(projectInfo, fs);
                }
                callback(new Error(Error.OK), projectInfo);
            }
        }

        public void DeleteProject(string projectPath, StorageEventHandler callback)
        {
            string projectDir = FullPath(projectPath);
            Directory.Delete(projectDir, true);
            callback(new Error(Error.OK));
        }

        public void GetProjects(StorageEventHandler<ProjectInfo[]> callback)
        {
            string projectsRoot = FullPath(string.Empty);
            string[] projectDirs = Directory.GetDirectories(projectsRoot);
            List<ProjectInfo> result = new List<ProjectInfo>();
            ISerializer serializer = IOC.Resolve<ISerializer>();
            for (int i = 0; i < projectDirs.Length; ++i)
            {
                string projectDir = projectDirs[i];
                if(File.Exists(projectDir + "/Project.rtmeta"))
                {
                    ProjectInfo projectInfo;
                    using (FileStream fs = File.OpenRead(projectDir + "/Project.rtmeta"))
                    {
                        projectInfo = serializer.Deserialize<ProjectInfo>(fs);
                    }
                    projectInfo.Name = Path.GetFileName(projectDir);
                    projectInfo.LastWriteTime = File.GetLastWriteTimeUtc(projectDir + "/Project.rtmeta");
                    result.Add(projectInfo);
                }
            }
            callback(new Error(Error.OK), result.ToArray());
        }

        public void GetProject(string projectName, StorageEventHandler<ProjectInfo, AssetBundleInfo[]> callback)
        {
            string projectDir = FullPath(projectName);
            string projectPath = projectDir + "/Project.rtmeta";
            ProjectInfo projectInfo;
            Error error = new Error();
            ISerializer serializer = IOC.Resolve<ISerializer>();
            AssetBundleInfo[] result = new AssetBundleInfo[0];
            if (!File.Exists(projectPath))
            {
                Directory.CreateDirectory(projectDir);
                using (FileStream fs = File.OpenWrite(projectDir + "/Project.rtmeta"))
                {
                    projectInfo = new ProjectInfo
                    {
                        Name = projectName,
                        LastWriteTime = DateTime.UtcNow
                    };

                    serializer.Serialize(projectInfo, fs);
                }
            }
            else
            {
                try
                {
                    using (FileStream fs = File.OpenRead(projectPath))
                    {
                        projectInfo = serializer.Deserialize<ProjectInfo>(fs);
                    }
                    projectInfo.Name = projectName;
                    projectInfo.LastWriteTime = File.GetLastWriteTimeUtc(projectPath);

                    string[] files = Directory.GetFiles(projectDir).Where(fn => fn.EndsWith(".rtbundle")).ToArray();
                    result = new AssetBundleInfo[files.Length];

                    for (int i = 0; i < result.Length; ++i)
                    {
                        using (FileStream fs = File.OpenRead(files[i]))
                        {
                            result[i] = serializer.Deserialize<AssetBundleInfo>(fs);
                        }
                    }
                }
                catch (Exception e)
                {
                    projectInfo = new ProjectInfo();
                    error.ErrorCode = Error.E_Exception;
                    error.ErrorText = e.ToString();
                }
            }

            callback(error, projectInfo, result);
        }

        public void GetProjectTree(string projectPath, StorageEventHandler<ProjectItem> callback)
        {
            projectPath = AssetsFolderPath(projectPath);
            if(!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }
            ProjectItem assets = new ProjectItem();
            assets.ItemID = 0;
            assets.Children = new List<ProjectItem>();
            assets.Name = "Assets";

            GetProjectTree(projectPath, assets);

            callback(new Error(), assets);
        }

        private static T LoadItem<T>(ISerializer serializer, string path) where T : ProjectItem, new()
        {
            T item = Load<T>(serializer, path);

            string fileNameWithoutMetaExt = Path.GetFileNameWithoutExtension(path);
            item.Name = Path.GetFileNameWithoutExtension(fileNameWithoutMetaExt);
            item.Ext = Path.GetExtension(fileNameWithoutMetaExt);

            return item;
        }
       
        private static T Load<T>(ISerializer serializer, string path) where T : new()
        {
            string metaFile = path;
            T item;
            if (File.Exists(metaFile))
            {
                try
                {
                    using (FileStream fs = File.OpenRead(metaFile))
                    {
                        item = serializer.Deserialize<T>(fs);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Unable to read meta file: {0} -> got exception: {1} ", metaFile, e.ToString());
                    item = new T();
                }
            }
            else
            {
                item = new T();
            }
         
            return item;
        }

        private void GetProjectTree(string path, ProjectItem parent)
        {
            if(!Directory.Exists(path))
            {
                return;
            }

            ISerializer serializer = IOC.Resolve<ISerializer>();
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; ++i)
            {
                string dir = dirs[i];
                ProjectItem projectItem = LoadItem<ProjectItem>(serializer, dir + MetaExt);

                projectItem.Parent = parent;
                projectItem.Children = new List<ProjectItem>();
                parent.Children.Add(projectItem);

                GetProjectTree(dir, projectItem);
            }

            string[] files = Directory.GetFiles(path, "*" + MetaExt);
            for(int i = 0; i < files.Length; ++i)
            {
                string file = files[i];
                if(!File.Exists(file.Replace(MetaExt, string.Empty)))
                {
                    continue;
                }

                AssetItem assetItem =  LoadItem<AssetItem>(serializer, file);
                assetItem.Parent = parent;
                parent.Children.Add(assetItem);
            }
        }

        public void GetPreviews(string projectPath, string[] assetPath, StorageEventHandler<Preview[]> callback)
        {
            projectPath = FullPath(projectPath);

            ISerializer serializer = IOC.Resolve<ISerializer>();
            Preview[] result = new Preview[assetPath.Length];
            for(int i = 0; i < assetPath.Length; ++i)
            {
                string path = projectPath + assetPath[i] + PreviewExt;
                if(File.Exists(path))
                {
                    result[i] = Load<Preview>(serializer, path);
                }
            }

            callback(new Error(), result);
        }

        public void GetPreviews(string projectPath, string[] folderPath, StorageEventHandler<Preview[][]> callback)
        {
            projectPath = FullPath(projectPath);

            ISerializer serializer = IOC.Resolve<ISerializer>();
            Preview[][] result = new Preview[folderPath.Length][];
            for (int i = 0; i < folderPath.Length; ++i)
            {
                string path = projectPath + folderPath[i];
                if (!Directory.Exists(path))
                {
                    continue;
                }

                string[] files = Directory.GetFiles(path, "*" + PreviewExt);
                Preview[] previews = new Preview[files.Length];
                for(int j = 0; j < files.Length; ++j)
                {
                    previews[j] = Load<Preview>(serializer, files[j]);
                }

                result[i] = previews;
            }

            callback(new Error(), result);
        }

        public void Save(string projectPath, string[] folderPaths, AssetItem[] assetItems, PersistentObject[] persistentObjects, ProjectInfo projectInfo, bool previewOnly, StorageEventHandler callback)
        {
            QueueUserWorkItem(() =>
            {
                if (!previewOnly)
                {
                    if (assetItems.Length != persistentObjects.Length)
                    {
                        throw new ArgumentException("assetItems");
                    }
                }

                if (assetItems.Length > folderPaths.Length)
                {
                    int l = folderPaths.Length;
                    Array.Resize(ref folderPaths, assetItems.Length);
                    for (int i = l; i < folderPaths.Length; ++i)
                    {
                        folderPaths[i] = folderPaths[l - 1];
                    }
                }

                projectPath = FullPath(projectPath);
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }

                string projectInfoPath = projectPath + "/Project.rtmeta";
                ISerializer serializer = IOC.Resolve<ISerializer>();
                Error error = new Error(Error.OK);
                for (int i = 0; i < assetItems.Length; ++i)
                {
                    string folderPath = folderPaths[i];
                    AssetItem assetItem = assetItems[i];

                    try
                    {
                        string path = projectPath + folderPath;
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        string previewPath = path + "/" + assetItem.NameExt + PreviewExt;
                        if (assetItem.Preview == null)
                        {
                            File.Delete(previewPath);
                        }
                        else
                        {
                            File.Delete(previewPath);
                            using (FileStream fs = File.Create(previewPath))
                            {
                                serializer.Serialize(assetItem.Preview, fs);
                            }
                        }

                        if (!previewOnly)
                        {
                            PersistentObject persistentObject = persistentObjects[i];
                            File.Delete(path + "/" + assetItem.NameExt + MetaExt);
                            using (FileStream fs = File.Create(path + "/" + assetItem.NameExt + MetaExt))
                            {
                                serializer.Serialize(assetItem, fs);
                            }

                            File.Delete(path + "/" + assetItem.NameExt);
                            using (FileStream fs = File.Create(path + "/" + assetItem.NameExt))
                            {
                                serializer.Serialize(persistentObject, fs);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Unable to create asset: {0} -> got exception: {1} ", assetItem.NameExt, e.ToString());
                        error.ErrorCode = Error.E_Exception;
                        error.ErrorText = e.ToString();
                        break;
                    }
                }

                File.Delete(projectInfoPath);
                using (FileStream fs = File.Create(projectInfoPath))
                {
                    serializer.Serialize(projectInfo, fs);
                }

                Callback(() => callback(error));
            });
        }

        public void Save(string projectPath, AssetBundleInfo assetBundleInfo, ProjectInfo projectInfo, StorageEventHandler callback)
        {
            QueueUserWorkItem(() =>
            {
                projectPath = FullPath(projectPath);
                string projectInfoPath = projectPath + "/Project.rtmeta";

                string assetBundlePath = assetBundleInfo.UniqueName.Replace("/", "_").Replace("\\", "_");
                assetBundlePath += ".rtbundle";
                assetBundlePath = projectPath + "/" + assetBundlePath;

                ISerializer serializer = IOC.Resolve<ISerializer>();

                using (FileStream fs = File.OpenWrite(assetBundlePath))
                {
                    serializer.Serialize(assetBundleInfo, fs);
                }

                using (FileStream fs = File.OpenWrite(projectInfoPath))
                {
                    serializer.Serialize(projectInfo, fs);
                }

                Callback(() => callback(new Error(Error.OK)));
            });
        }

        public void Load(string projectPath, string[] assetPaths, Type[] types, StorageEventHandler<PersistentObject[]> callback)
        {
            QueueUserWorkItem(() =>
            {
                PersistentObject[] result = new PersistentObject[assetPaths.Length];
                for (int i = 0; i < assetPaths.Length; ++i)
                {
                    string assetPath = assetPaths[i];
                    assetPath = FullPath(projectPath) + assetPath;
                    ISerializer serializer = IOC.Resolve<ISerializer>();
                    try
                    {
                        if (File.Exists(assetPath))
                        {
                            using (FileStream fs = File.OpenRead(assetPath))
                            {
                                result[i] = (PersistentObject)serializer.Deserialize(fs, types[i]);
                            }
                        }
                        else
                        {
                            Callback(() => callback(new Error(Error.E_NotFound), new PersistentObject[0]));
                            return;
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Unable to load asset: {0} -> got exception: {1} ", assetPath, e.ToString());
                        Callback(() => callback(new Error(Error.E_Exception) { ErrorText = e.ToString() }, new PersistentObject[0]));
                        return;
                    }
                }

                Callback(() => callback(new Error(Error.OK), result));
            });
           
        }

        public void Load(string projectPath, string bundleName, StorageEventHandler<AssetBundleInfo> callback)
        {
            QueueUserWorkItem(() =>
            {
                string assetBundleInfoPath = bundleName.Replace("/", "_").Replace("\\", "_");
                assetBundleInfoPath += ".rtbundle";
                assetBundleInfoPath = FullPath(projectPath) + "/" + assetBundleInfoPath;

                ISerializer serializer = IOC.Resolve<ISerializer>();
                if (File.Exists(assetBundleInfoPath))
                {
                    AssetBundleInfo result = null;
                    using (FileStream fs = File.OpenRead(assetBundleInfoPath))
                    {
                        result = serializer.Deserialize<AssetBundleInfo>(fs);
                    }

                    Callback(() => callback(new Error(Error.OK), result));
                }
                else
                {
                    Callback(() => callback(new Error(Error.E_NotFound), null));
                }
            });
        }



        public void Delete(string projectPath, string[] paths, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            for(int i = 0; i < paths.Length; ++i)
            {
                string path = fullPath + paths[i];
                if(File.Exists(path))
                {
                    File.Delete(path);
                    if(File.Exists(path + MetaExt))
                    {
                        File.Delete(path + MetaExt);
                    }
                    if(File.Exists(path + PreviewExt))
                    {
                        File.Delete(path + PreviewExt);
                    }
                }
                else if(Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            callback(new Error(Error.OK));
        }

        public void Rename(string projectPath, string[] paths, string[] oldNames, string[] names, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            for (int i = 0; i < paths.Length; ++i)
            {
                string path = fullPath + paths[i] + "/" + oldNames[i];
                if (File.Exists(path))
                {
                    File.Move(path, fullPath + paths[i] + "/" + names[i]);
                    if (File.Exists(path + MetaExt))
                    {
                        File.Move(path + MetaExt, fullPath + paths[i] + "/" + names[i] + MetaExt);
                    }
                    if (File.Exists(path + PreviewExt))
                    {
                        File.Move(path + PreviewExt, fullPath + paths[i] + "/" + names[i] + PreviewExt);
                    }
                }
                else if (Directory.Exists(path))
                {
                    Directory.Move(path, fullPath + paths[i] + "/" + names[i]);
                }
            }

            callback(new Error(Error.OK));
        }

        public void Move(string projectPath, string[] paths, string[] names, string targetPath, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            for (int i = 0; i < paths.Length; ++i)
            {
                string path = fullPath + paths[i] + "/" + names[i];
                if (File.Exists(path))
                {
                    File.Move(path, fullPath + targetPath + "/" + names[i]);
                    if (File.Exists(path + MetaExt))
                    {
                        File.Move(path + MetaExt, fullPath + targetPath + "/" + names[i] + MetaExt);
                    }
                    if (File.Exists(path + PreviewExt))
                    {
                        File.Move(path + PreviewExt, fullPath + targetPath + "/" + names[i] + PreviewExt);
                    }
                }
                else if (Directory.Exists(path))
                {
                    Directory.Move(path, fullPath + targetPath + "/" + names[i]);
                }
            }

            callback(new Error(Error.OK));
        }

        public void Create(string projectPath, string[] paths, string[] names, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            for (int i = 0; i < paths.Length; ++i)
            {
                string path = fullPath + paths[i] + "/" + names[i];
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            callback(new Error(Error.OK));
        }

        
        public void GetValue(string projectPath, string key, Type type, StorageEventHandler<PersistentObject> callback)
        {
            string fullPath = FullPath(projectPath);
            string path = fullPath + "/" + KeyValueStorage;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "/" + key;
            if (File.Exists(path))
            {
                ISerializer serializer = IOC.Resolve<ISerializer>();
                object result = null;
                using (FileStream fs = File.OpenRead(path))
                {
                    result = serializer.Deserialize(fs, type);
                }

                callback(new Error(Error.OK), (PersistentObject)result);
            }
            else
            {
                callback(new Error(Error.E_NotFound), null);
                return;
            }
        }

        public void GetValues(string projectPath, string searchPattern, Type type, StorageEventHandler<PersistentObject[]> callback)
        {
            QueueUserWorkItem(() =>
            {
                string fullPath = FullPath(projectPath);
                string path = fullPath + "/" + KeyValueStorage;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string[] files = Directory.GetFiles(path, searchPattern);
                PersistentObject[] result = new PersistentObject[files.Length];

                ISerializer serializer = IOC.Resolve<ISerializer>();
                for (int i = 0; i < files.Length; ++i)
                {
                    using (FileStream fs = File.OpenRead(files[i]))
                    {
                        result[i] = (PersistentObject)serializer.Deserialize(fs, type);
                    }
                }

                Callback(() => callback(Error.NoError, result));
            });
        }

        public void SetValue(string projectPath, string key, PersistentObject persistentObject, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            string path = fullPath + "/" + KeyValueStorage;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "/" + key;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ISerializer serializer = IOC.Resolve<ISerializer>();
            using (FileStream fs = File.Create(path))
            {
                serializer.Serialize(persistentObject, fs);
            }
            serializer.Serialize(persistentObject);

            callback(new Error(Error.OK));
        }

        public void DeleteValue(string projectPath, string key, StorageEventHandler callback)
        {
            string fullPath = FullPath(projectPath);
            string path = fullPath + "/" + KeyValueStorage + "/" + key;
            File.Delete(path);
            callback(Error.NoError);
        }

        public void QueueUserWorkItem(Action action)
        {
#if UNITY_WEBGL
            action();
#else
            if (Dispatcher.Current != null)
            {
                ThreadPool.QueueUserWorkItem(arg =>
                {
                    try
                    {
                        action();
                    }
                    catch(Exception e)
                    {
                        Dispatcher.BeginInvoke(() => Debug.LogError(e));
                    }
                    
                });
            }
            else
            {
                action();
            }
#endif
        }

        public void Callback(Action callback)
        {
            Dispatcher.BeginInvoke(callback);
        }
    }
}
