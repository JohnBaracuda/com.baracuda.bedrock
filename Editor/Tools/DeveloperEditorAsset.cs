using System;
using UnityEngine;

namespace Baracuda.Bedrock.Editor.Tools
{
    /// <summary>
    ///     Base type for developer specific configuration files.
    /// </summary>
    public abstract class DeveloperEditorAsset<T> : ScriptableObject where T : DeveloperEditorAsset<T>
    {
        private static T local;

        public static T Local
        {
            get
            {
                if (local == null)
                {
                    var guid = UnityEditor.EditorPrefs.GetString(typeof(T).FullName);
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

                    if (asset != null)
                    {
                        Local = asset;
                    }
                }

                if (local == null)
                {
                    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Settings"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", "Settings");
                    }

                    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Settings/Developer"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets/Settings", "Developer");
                    }

                    var asset = CreateInstance<T>();
                    var assetPath = $"Assets/Settings/Developer/{typeof(T).Name}.{Environment.UserName.Trim()}.asset";
                    UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log("Singleton", $"Creating new {typeof(T).Name} instance at {assetPath}!");
                    Local = asset;
                }

                return local;
            }
            private set
            {
                if (value == null)
                {
                    return;
                }

                var path = UnityEditor.AssetDatabase.GetAssetPath(value);
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                UnityEditor.EditorPrefs.SetString(typeof(T).FullName, guid);
                local = value;
            }
        }
    }
}