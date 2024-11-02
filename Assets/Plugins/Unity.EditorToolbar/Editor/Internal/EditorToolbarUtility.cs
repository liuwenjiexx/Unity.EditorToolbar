using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorToolbars
{
    public static partial class EditorToolbarUtility
    {
        private static string unityPackageName;
        internal static string UnityPackageName
        {
            get
            {
                if (unityPackageName == null)
                {
                    unityPackageName = GetUnityPackageName(typeof(EditorToolbarUtility).Assembly);
                }
                return unityPackageName;
            }
        }

        private static string unityPackageDir;

        internal static string UnityPackageDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(unityPackageDir))
                    unityPackageDir = GetUnityPackageDirectory(UnityPackageName);
                return unityPackageDir;
            }
        }
        static Dictionary<string, string> unityPackageDirectories = new Dictionary<string, string>();

        internal static string GetUnityPackageName(Assembly assembly)
        {
            return GetAssemblyMetadata(assembly, "Unity.Package.Name");
        }

        internal static string GetUnityPackageDirectory(Assembly assembly)
        {
            return GetUnityPackageDirectory(GetUnityPackageName(assembly));
        }

        //2021/4/13
        internal static string GetUnityPackageDirectory(string packageName)
        {
            if (!unityPackageDirectories.TryGetValue(packageName, out var path))
            {
                var tmp = Path.Combine("Packages", packageName);
                if (Directory.Exists(tmp) && File.Exists(Path.Combine(tmp, "package.json")))
                {
                    path = tmp;
                }

                if (path == null)
                {
                    foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
                    {
                        if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (File.Exists(Path.Combine(dir, "package.json")))
                            {
                                path = dir;
                                break;
                            }
                        }
                    }
                }

                if (path == null)
                {
                    foreach (var pkgPath in Directory.GetFiles("Assets", "package.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (JsonUtility.FromJson<_UnityPackage>(File.ReadAllText(pkgPath, System.Text.Encoding.UTF8)).name == packageName)
                            {
                                path = Path.GetDirectoryName(pkgPath);
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (path != null)
                {
                    path = path.Replace('\\', '/');
                }
                unityPackageDirectories[packageName] = path;
            }
            return path;
        }

        [Serializable]
        class _UnityPackage
        {
            public string name;
        }

        #region Assembly Metadata

        static Dictionary<(Assembly, string), (string, bool)> assemblyMetadatas;

        internal static string GetAssemblyMetadata(Assembly assembly, string key)
        {
            if (!TryGetAssemblyMetadata(assembly, key, out var value))
                throw new Exception($"Not found AssemblyMetadataAttribute. key: {key}");
            return value;
        }

        internal static string GetAssemblyMetadata(Assembly assembly, string key, string defaultValue)
        {
            if (!TryGetAssemblyMetadata(assembly, key, out var value))
            {
                value = defaultValue;
            }
            return value;
        }

        internal static bool TryGetAssemblyMetadata(Assembly assembly, string key, out string value)
        {
            if (assemblyMetadatas == null)
                assemblyMetadatas = new Dictionary<(Assembly, string), (string, bool)>();

            if (!assemblyMetadatas.TryGetValue((assembly, key), out var item))
            {
                foreach (var attr in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                {
                    if (attr.Key == key)
                    {
                        item = new(attr.Value, true);
                        break;
                    }
                }

                assemblyMetadatas[(assembly, key)] = item;
            }

            if (item.Item2)
            {
                value = item.Item1;
                return true;
            }
            value = null;
            return false;
        }

        #endregion


        #region UXML

        internal static string GetUSSPath(string uss, Type type = null)
        {
            string dir = GetUnityPackageDirectory((type ?? typeof(EditorToolbarUtility)).Assembly);
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}/Editor/USS/{uss}.uss";
        }

        internal static string GetUXMLPath(string uxml, Type type = null)
        {
            string dir = GetUnityPackageDirectory((type ?? typeof(EditorToolbarUtility)).Assembly);
            return $"{dir}/Editor/UXML/{uxml}.uxml";
        }

        internal static StyleSheet AddStyle(VisualElement elem, Type type, string uss = null)
        {
            if (uss == null)
            {
                uss = type.Name;
            }
            string assetPath = GetUSSPath(uss, type);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (style)
            {
                elem.styleSheets.Add(style);
            }
            return style;
        }

        internal static TemplateContainer LoadUXML(string uxml, VisualElement parent = null)
        {
            return LoadUXML(typeof(EditorToolbarUtility), uxml, parent);
        }
        internal static TemplateContainer LoadUXML(Type type, VisualElement parent = null)
        {
            return LoadUXML(type, null, parent);
        }
        internal static TemplateContainer LoadUXML(Type type, string uxml, VisualElement parent = null)
        {
            if (uxml == null)
            {
                uxml = type.Name;
            }
            string path = GetUXMLPath(uxml, type);
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            TemplateContainer treeRoot = null;
            if (asset)
            {
                treeRoot = asset.CloneTree();
                if (parent != null)
                {
                    parent.Add(treeRoot);
                }
            }
            return treeRoot;
        }

        #endregion

         

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var obj = Selection.activeObject;
                if (obj != null && obj is ToolbarInspectorObject)
                {
                    Selection.activeObject = null;
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        private static void CompilationPipeline_compilationStarted(object e)
        {
            var obj = Selection.activeObject;
            if (obj != null && obj is ToolbarInspectorObject)
            {
                Selection.activeObject = null;
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}