using System;
using System.Collections.Generic;
using System.IO;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Serialization;
using Baracuda.Utilities;
using Baracuda.Utilities.Events;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Baracuda.Bedrock.Values
{
    public abstract class SaveDataAsset<TValue> : SaveDataAsset, ISerializationCallbackReceiver
    {
        [LabelText("Default Value")]
        [SerializeField] private TValue defaultPersistentValue;
        [NonSerialized] private readonly Broadcast<TValue> _changedEvent = new();

        [PublicAPI]
        [ShowInInspector]
        public TValue Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        [PublicAPI]
        public void SetValue(TValue value)
        {
            Assert.IsFalse(Key.IsNullOrWhitespace(), $"Save data asset key of {name} is not set!");
            if (EqualityComparer<TValue>.Default.Equals(value, GetValue()))
            {
                return;
            }
            Profile.SaveFile(Key, value, new StoreOptions(Tags));
            _changedEvent.Raise(value);
        }

        [PublicAPI]
        public TValue GetValue()
        {
            Assert.IsFalse(Key.IsNullOrWhitespace(), $"Save data asset key of {name} is not set!");
            return Profile.LoadFile<TValue>(Key, new StoreOptions(Tags));
        }

        [PublicAPI]
        public event Action<TValue> Changed
        {
            add => _changedEvent.Add(value);
            remove => _changedEvent.Remove(value);
        }


        #region Persistent Data

        private ISaveProfile Profile => StorageLevel switch
        {
            StorageLevel.Profile => FileSystem.Profile,
            StorageLevel.SharedProfile => FileSystem.SharedProfile,
            var _ => throw new ArgumentOutOfRangeException()
        };

        [Line]
        [Button]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 8)]
        private void OpenInFileSystem()
        {
            var dataPath = Application.persistentDataPath;
            var systemPath = FileSystem.RootFolder;
            var profilePath = Profile.Info.FolderName;
            var folderPath = Path.Combine(dataPath, systemPath, profilePath);
            Application.OpenURL(folderPath);
        }

        [Button("Reset")]
        [ButtonGroup("Persistent")]
        public override void ResetPersistentData()
        {
            Value = defaultPersistentValue;
            _changedEvent.Raise(Value);
        }

        #endregion


        #region Initialization

        private void OnEnable()
        {
            UpdateSaveDataKey();
            FileSystem.InitializationCompleted += OnFileSystemInitialized;

            if (FileSystem.IsInitialized)
            {
                OnFileSystemInitialized();
            }

#if UNITY_EDITOR
            Gameloop.BeforeDeleteAsset += (path, asset) =>
            {
                if (asset == this)
                {
                    FileSystem.InitializationCompleted -= OnFileSystemInitialized;
                }
            };
#endif

            void OnFileSystemInitialized()
            {
                if (Profile.HasFile(Key) is false)
                {
                    SetValue(defaultPersistentValue);
                }

                _changedEvent.Raise(Value);
            }
        }

        public void OnBeforeSerialize()
        {
            UpdateSaveDataKey();
        }

        public void OnAfterDeserialize()
        {
        }

        #endregion
    }
}