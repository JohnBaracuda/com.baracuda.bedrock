using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Serialization;
using Baracuda.Utilities.Events;
using Baracuda.Utilities.Types;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Baracuda.Bedrock.Values
{
    public abstract partial class ValueAsset<TValue>
    {
        #region Inspector

        [Line]
        [SerializeField] private ValueAssetType valueAssetType = ValueAssetType.Runtime;
        [SerializeField] private bool enableEvents = true;

        // Serialized Value
        [Line]
        [LabelText("@name", NicifyText = true)]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Constant)]
        [SerializeField] private TValue serializedValue;

        // Runtime Value
        [Line]
        [LabelText("Default Value")]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Runtime)]
        [DisableIf("@UnityEngine.Application.isPlaying")]
        [Tooltip("The default value for the runtime value. The runtime value will be reset to this value")]
        [OnValueChanged(nameof(OnUpdateRuntimeValue))]
        [SerializeField] private TValue defaultRuntimeValue;

        // Persistent Value
        [Line]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        [LabelText("Default Value")]
        [SerializeField] private TValue defaultPersistentValue;
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        [SerializeField] private RuntimeGUID guid;
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        [Tooltip("The level to store the data on. Either profile specific or shared between profiles")]
        [SerializeField] private StorageLevel storageLevel = StorageLevel.Profile;
        [Tooltip("When enabled, the value is always saved when updated")]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        [SerializeField] private bool autoSave = true;

        // Property
        [Line]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Property)]
        [SerializeField] private bool logPropertyWarnings;

        [ShowInInspector]
        [PropertyOrder(2)]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Property)]
        private bool HasSetter => _setter != null;

        [ShowInInspector]
        [PropertyOrder(2)]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Property)]
        private bool HasGetter => _getter != null;

        #endregion


        #region Fields

        [NonSerialized] private TValue _runtimeValue;
        [NonSerialized] private TValue _persistentValue;

        private Func<TValue> _getter;
        private Action<TValue> _setter;

        private readonly Broadcast<TValue> _changedEvent = new();

        #endregion


        #region Initialization

        [CallbackOnEnterEditMode]
        private void Shutdown()
        {
            switch (valueAssetType)
            {
                case ValueAssetType.Constant:
                    break;

                case ValueAssetType.Runtime:
                    _runtimeValue = defaultRuntimeValue;
                    break;

                case ValueAssetType.Save:
                    SavePersistentData();
                    break;

                case ValueAssetType.Property:
                    _setter = null;
                    _getter = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion


        #region Get & Set

        public override TValue GetValue()
        {
            return valueAssetType switch
            {
                ValueAssetType.Constant => serializedValue,
                ValueAssetType.Runtime => _runtimeValue,
                ValueAssetType.Save => _persistentValue,
                ValueAssetType.Property => GetPropertyValue(),
                var _ => throw new InvalidOperationException()
            };

            TValue GetPropertyValue()
            {
                if (_getter is null)
                {
#if DEBUG
                    if (logPropertyWarnings)
                    {
                        Debug.LogWarning("Value Asset", "Property getter is not set!", this);
                    }
#endif
                    return default;
                }
                return _getter();
            }
        }

        public override void SetValue(TValue value)
        {
            var raiseChangedEvent = enableEvents && _changedEvent.Count > 0 && AreNotEqual(Value, value);

            switch (valueAssetType)
            {
                case ValueAssetType.Constant:
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning("Value Asset", "Setting a constant value during runtime!", this);
                        return;
                    }
#endif
                    serializedValue = value;
                    if (raiseChangedEvent)
                    {
                        _changedEvent.Raise(value);
                    }
                    break;

                case ValueAssetType.Runtime:
                    _runtimeValue = value;
                    if (raiseChangedEvent)
                    {
                        _changedEvent.Raise(value);
                    }
                    break;

                case ValueAssetType.Save:
                    _persistentValue = value;
                    if (autoSave)
                    {
                        SavePersistentData();
                    }
                    if (raiseChangedEvent)
                    {
                        _changedEvent.Raise(value);
                    }
                    break;

                case ValueAssetType.Property:
                    if (_setter is null)
                    {
#if DEBUG
                        if (logPropertyWarnings)
                        {
                            Debug.LogWarning("Mediator", "Property setter is not set!", this);
                        }
#endif
                        break;
                    }
                    _setter.Invoke(value);
                    if (raiseChangedEvent)
                    {
                        _changedEvent.Raise(value);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SaveInternal()
        {
            switch (valueAssetType)
            {
#if UNITY_EDITOR
                case ValueAssetType.Constant:
                    UnityEditor.EditorUtility.SetDirty(this);
                    break;
#endif
                case ValueAssetType.Save:
                    SavePersistentData();
                    break;

                case ValueAssetType.Property:
                    _changedEvent.Raise(Value);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AreEqual(in TValue first, in TValue second)
        {
            return EqualityComparer<TValue>.Default.Equals(first, second);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AreNotEqual(in TValue first, in TValue second)
        {
            return !EqualityComparer<TValue>.Default.Equals(first, second);
        }

        #endregion


        #region Property Binding

        private void BindGetterInternal(Func<TValue> getter)
        {
            _getter = getter;
        }

        private void ReleaseGetterInternal(Func<TValue> getter)
        {
            Assert.AreEqual(getter, _getter);
            _getter = null;
        }

        private void BindSetterInternal(Action<TValue> setter)
        {
            _setter = setter;
        }

        private void ReleaseSetterInternal(Action<TValue> setter)
        {
            Assert.AreEqual(setter, _setter);
            _setter = null;
        }

        #endregion


        #region Persistent Data

        private ISaveProfile Profile => storageLevel switch
        {
            StorageLevel.Profile => FileSystem.Profile,
            StorageLevel.SharedProfile => FileSystem.SharedProfile,
            var _ => throw new ArgumentOutOfRangeException()
        };

        private string Key => guid.ToString();

        [Line]
        [Button]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 8)]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        private void OpenInFileSystem()
        {
            var dataPath = Application.persistentDataPath;
            var systemPath = FileSystem.RootFolder;
            var profilePath = Profile.Info.FolderName;
            var folderPath = Path.Combine(dataPath, systemPath, profilePath);
            Application.OpenURL(folderPath);
        }

        [Button("Save")]
        [ButtonGroup("Persistent")]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        public void SavePersistentData()
        {
            Profile.SaveFile(Key, _persistentValue);
        }

        [Button("Load")]
        [ButtonGroup("Persistent")]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        public void LoadPersistentData()
        {
            if (!Profile.TryLoadFile(Key, out _persistentValue))
            {
                _persistentValue = defaultPersistentValue;
            }
        }

        [Button("Reset")]
        [ButtonGroup("Persistent")]
        [ShowIf(nameof(valueAssetType), ValueAssetType.Save)]
        public void ResetPersistentData()
        {
            _persistentValue = defaultPersistentValue;
            SavePersistentData();
        }

        #endregion


        #region Editor

        private void OnUpdateRuntimeValue()
        {
            if (Application.isPlaying is false)
            {
                _runtimeValue = defaultRuntimeValue;
            }
        }

        #endregion


        #region Enable & Disable

        protected override void OnEnable()
        {
            _persistentValue = defaultPersistentValue;
#if UNITY_EDITOR
            RuntimeGUID.Create(this, ref guid);
#endif
            switch (valueAssetType)
            {
                case ValueAssetType.Constant:
                    break;

                case ValueAssetType.Runtime:
                    _runtimeValue = defaultRuntimeValue;
                    break;

                case ValueAssetType.Save:
                    if (FileSystem.IsInitialized)
                    {
                        LoadPersistentData();
                    }
                    break;

                case ValueAssetType.Property:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.OnEnable();
        }

        #endregion
    }
}