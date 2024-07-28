using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.Services;
using Baracuda.Utilities.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace Baracuda.Bedrock.Cursor
{
    public class CursorSet : ScriptableObject
    {
        [SerializeField] [Required] private CursorFile fallback;
        [Space]
        [SerializeField] private Map<CursorType, CursorFile> cursorMappings;

        public CursorFile GetCursor(CursorType type)
        {
            if (cursorMappings.TryGetValue(type, out var file) && file != null)
            {
                return file;
            }

            Debug.LogWarning("Cursor Set", $"Unable to find cursor file for {type.name} ({type.GetInstanceID()}) in {name}!");

            return fallback;
        }

        public CursorType GetType(CursorFile file)
        {
            foreach (var (cursorType, cursorFile) in cursorMappings)
            {
                if (file == cursorFile)
                {
                    return cursorType;
                }
            }

            Debug.LogWarning("Cursor Set", $"Unable to find cursor type for {file.name} in {name}!");
            return CursorType.None;
        }

#if UNITY_EDITOR

        [Button]
        [Line]
        private void Initialize()
        {
            var assets = ListPool<CursorType>.Get();
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(CursorType)}");
            foreach (var guid in guids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CursorType>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            foreach (var cursorType in assets)
            {
                cursorMappings.TryAdd(cursorType, null);
            }

            ListPool<CursorType>.Release(assets);
        }

        [Button]
        private void Clear()
        {
            cursorMappings.Clear();
        }

        [Button]
        private void Activate()
        {
            if (Application.isPlaying)
            {
                ServiceLocator.Get<CursorManager>().SwitchActiveCursorSet(this);
            }
        }
#endif
    }
}