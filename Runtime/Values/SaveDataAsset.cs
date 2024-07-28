using System.Diagnostics;
using Baracuda.Serialization;
using Baracuda.Utilities;
using Baracuda.Utilities.Types;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Baracuda.Bedrock.Values
{
    public abstract class SaveDataAsset : ScriptableObject
    {
        [AssetGUID]
        [SerializeField] private string key;
        [ListDrawerSettings(DefaultExpandedState = false)]
        [SerializeField] private string[] tags;
        [SerializeField] private StorageLevel storageLevel = StorageLevel.Profile;

        [PublicAPI]
        public string Key => key;

        [PublicAPI]
        public string[] Tags => tags;

        [PublicAPI]
        public StorageLevel StorageLevel => storageLevel;

        public abstract void ResetPersistentData();

        [Conditional("UNITY_EDITOR")]
        protected void UpdateSaveDataKey()
        {
#if UNITY_EDITOR
            if (key.IsNullOrWhitespace())
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                key = guid;
            }
#endif
        }
    }
}