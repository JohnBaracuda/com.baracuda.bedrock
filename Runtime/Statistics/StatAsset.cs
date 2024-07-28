using System;
using System.Linq;
using Baracuda.Bedrock.Odin;
using Baracuda.Serialization;
using Baracuda.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Baracuda.Bedrock.Statistics
{
    public abstract class StatAsset<T> : StatAsset
    {
        #region Fields & Properties

        [ReadOnly]
        [SerializeField] private string guid;
        [NonSerialized] private StatData<T> _statData = StatData<T>.Empty;

        public event Action<T> Changed
        {
            add => _changedBroadcast.Add(value);
            remove => _changedBroadcast.Remove(value);
        }

        [ReadOnly]
        [ShowInInspector]
        [Foldout("Settings")]
        public T Value => _statData != null ? _statData.value : default;

        protected StatData<T> StatData => _statData;
        private readonly Broadcast<T> _changedBroadcast = new();
        private static readonly StoreOptions storeOptions = new("Statistics", typeof(T).Name);

        #endregion


        #region Setup

        protected void OnEnable()
        {
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif
            FileSystem.InitializationCompleted += Initialize;
            if (FileSystem.IsInitialized)
            {
                Initialize();
            }
        }

        private void Reset()
        {
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            if (_statData != null)
            {
                _statData.description = Description;
                _statData.name = Name;
                _statData.type = Type();
            }
#endif
        }

        private void Initialize()
        {
            Profile.TryLoadFile(guid, out _statData);
            _statData ??= new StatData<T>(guid, Name, Description, DefaultValue(), Type());

            _statData.description = Description;
            _statData.name = Name;
            _statData.type = Type();

            Profile.SaveFile(guid, _statData, storeOptions);
        }

        #endregion


        #region Protected

        protected abstract T DefaultValue();

        protected void SetStatDirty()
        {
            _changedBroadcast.Raise(Value);
            Profile.SaveFile(guid, _statData, storeOptions);
            Updated.Raise(this);
#if UNITY_EDITOR
            if (Repaint && UnityEditor.Selection.objects.Contains(this))
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        #endregion
    }
}