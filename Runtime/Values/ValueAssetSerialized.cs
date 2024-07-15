using System;
using Baracuda.Bedrock.Odin;
using Baracuda.Utilities.Events;
using UnityEngine;

namespace Baracuda.Bedrock.Values
{
    public abstract class ValueAssetSerialized<TValue> : ValueAssetRW<TValue>,
        IValueAsset<TValue>,
        IObservable<TValue>
    {
        [Line]
        [SerializeField] private TValue value;

        [NonSerialized] private readonly Broadcast<TValue> _changedEvent = new();

        /// <summary>
        ///     Called when the value was changed.
        /// </summary>
        public override event Action<TValue> Changed
        {
            add => _changedEvent.Add(value);
            remove => _changedEvent.Remove(value);
        }

        public override TValue Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public override void SetValue(TValue newValue)
        {
            if (Application.isPlaying)
            {
                return;
            }
            value = newValue;
        }

        public override TValue GetValue()
        {
            return value;
        }
    }
}