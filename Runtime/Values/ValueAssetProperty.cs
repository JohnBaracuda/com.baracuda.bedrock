using System;
using Baracuda.Bedrock.Odin;
using Baracuda.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Baracuda.Bedrock.Values
{
    public abstract class ValueAssetProperty<TValue> : ValueAssetRW<TValue>, IValueAsset<TValue>, IPropertyAsset<TValue>
    {
        [Line]
        [SerializeField] private bool logPropertyWarnings;

        [ShowInInspector]
        [PropertyOrder(2)]
        private bool HasSetter => _setter != null;

        [ShowInInspector]
        [PropertyOrder(2)]
        private bool HasGetter => _getter != null;

        private Func<TValue> _getter;
        private Action<TValue> _setter;

        private readonly Broadcast<TValue> _changedEvent = new();

        [ShowInInspector]
        public override TValue Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        ///     Called when the value was changed.
        /// </summary>
        public override event Action<TValue> Changed
        {
            add => _changedEvent.Add(value);
            remove => _changedEvent.Remove(value);
        }

        public override void SetValue(TValue value)
        {
            if (_setter is null)
            {
#if DEBUG
                if (logPropertyWarnings)
                {
                    Debug.LogWarning("Mediator", "Property setter is not set!", this);
                }
#endif
                return;
            }
            _setter.Invoke(value);
            _changedEvent.Raise(value);
        }

        public override TValue GetValue()
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

        public void BindGetter(Func<TValue> getter)
        {
            _getter = getter;
        }

        public void ReleaseGetter(Func<TValue> getter)
        {
            Assert.AreEqual(getter, _getter);
            _getter = null;
        }

        public void BindSetter(Action<TValue> setter)
        {
            _setter = setter;
        }

        public void ReleaseSetter(Action<TValue> setter)
        {
            Assert.AreEqual(setter, _setter);
            _setter = null;
        }

        public void SetPropertyDirty()
        {
            _changedEvent.Raise(Value);
        }
    }
}