﻿using System.Collections.Generic;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.PlayerLoop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Baracuda.Bedrock.Collections
{
    public abstract class RuntimeCollectionAsset<T> : ScriptableObject
    {
#pragma warning disable
        [Foldout("Options")]
        [Tooltip(
            "When enabled, leaks that occur when exiting playmode will logged to the console. A leak occurs when a collection is not empty after transitioning from play to edit mode.")]
        [SerializeField] private bool logLeaks = true;
        [Tooltip(
            "When enabled, leaks that occur when exiting playmode will be cleared automatically. A leak occurs when a collection is not empty after transitioning from play to edit mode.")]
        [SerializeField] private bool clearLeaks = true;
        [Tooltip("When enabled, changes to the collection will trigger an immediate repaint in the inspector")]
        [SerializeField] private bool allowRepaint = true;

        private protected abstract int CountInternal { get; }
        private protected abstract IEnumerable<T> CollectionInternal { get; }

        [CallbackOnEnterEditMode]
        private void OnEnterEditMode()
        {
            if (logLeaks && CountInternal > 0)
            {
                Debug.LogWarning("Collection",
                    $"Leak detected in runtime collection: {name}\n{CollectionInternal.ToCollectionString()} " +
                    "This means that you did not remove every element from the collection during shutdown! " +
                    "Please ensure that runtime collections are properly shutdown and cleared!", this);
            }

            if (clearLeaks && CountInternal > 0)
            {
                ClearInternal();
            }
        }

        [Button("Clear")]
        [Foldout("Options")]
        private protected abstract void ClearInternal();
    }
}