using System;
using System.Collections.Generic;
using System.Linq;
using Baracuda.Bedrock.Mediator;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Utilities;
using Baracuda.Utilities.Events;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Baracuda.Bedrock.Locks
{
    /// <summary>
    ///     Generic variant of a <see cref="LockAsset{T}" />.
    ///     Use this for more control over what can and what cannot block.
    /// </summary>
    /// <typeparam name="T">Instances of this type can be used as lock</typeparam>
    public abstract class LockAsset<T> : MediatorAsset
    {
        #region Inspector

        [Foldout("Options")]
        [Tooltip("When enabled, leaks that occur when exiting playmode will logged to the console")]
        [SerializeField] private bool logLeaks = true;
        [Tooltip("When enabled, leaks that occur when exiting playmode will be cleared automatically")]
        [SerializeField] private bool clearLeaks = true;

        [ReadOnly]
        [ShowInInspector]
        private readonly HashSet<T> _locks = new(4);
        private readonly Broadcast _firstAddedEvent = new();
        private readonly Broadcast _anyAddedEvent = new();
        private readonly Broadcast _lastRemovedEvent = new();
        private readonly Broadcast _anyRemovedEvent = new();

        #endregion


        #region Public

        /// <summary>
        ///     Event is invoked when the first instance is added.
        /// </summary>
        [PublicAPI]
        public event Action FirstAdded
        {
            add
            {
                _firstAddedEvent.Add(value);
                if (HasAny())
                {
                    value();
                }
            }
            remove => _firstAddedEvent.Remove(value);
        }

        /// <summary>
        ///     Event is invoked when all instances were removed.
        /// </summary>
        [PublicAPI]
        public event Action LastRemoved
        {
            add
            {
                _lastRemovedEvent.Add(value);
                if (HasAny() is false)
                {
                    value();
                }
            }
            remove => _lastRemovedEvent.Remove(value);
        }

        /// <summary>
        ///     Event is invoked when any instance is added.
        /// </summary>
        [PublicAPI]
        public event Action AnyAdded
        {
            add => _anyAddedEvent.Add(value);
            remove => _anyAddedEvent.Remove(value);
        }

        /// <summary>
        ///     Event is invoked when any instances was removed.
        /// </summary>
        [PublicAPI]
        public event Action AnyRemoved
        {
            add => _anyRemovedEvent.Add(value);
            remove => _anyRemovedEvent.Remove(value);
        }

        /// <summary>
        ///     Returns true if any object is currently registered.
        /// </summary>
        [PublicAPI]
        public bool HasAny()
        {
            return _locks.Any();
        }

        /// <summary>
        ///     Returns true if the passed object is registered.
        /// </summary>
        [PublicAPI]
        public bool IsObjectProviding(T potentialLock)
        {
            return _locks.Contains(potentialLock);
        }

        /// <summary>
        ///     AddSingleton a new object to the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was added, false if it was already added</returns>
        [PublicAPI]
        public bool Add(T instance)
        {
            var wasAdded = _locks.Add(instance);
            if (wasAdded && _locks.Count == 1)
            {
                _firstAddedEvent.Raise();
            }
            _anyAddedEvent.Raise();
            Repaint();
            return wasAdded;
        }

        /// <summary>
        ///     Remove an object from the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was removed, false if it was not an active lock</returns>
        [PublicAPI]
        public bool Remove(T instance)
        {
            var wasRemoved = _locks.Remove(instance);
            if (wasRemoved && _locks.Count == 0)
            {
                _lastRemovedEvent.Raise();
            }
            _anyRemovedEvent.Raise();
            Repaint();
            return wasRemoved;
        }

        /// <summary>
        ///     Remove all providing objects and release the lock.
        /// </summary>
        /// <param name="discrete">When true, no events are raised.</param>
        /// <returns>the amount of removed provider</returns>
        [PublicAPI]
        public int ReleaseAll(bool discrete = false)
        {
            var count = _locks.Count;
            _locks.Clear();
            if (count > 0 && discrete is false)
            {
                _lastRemovedEvent.Raise();
            }
            return count;
        }

        #endregion


        #region Operator

        public static explicit operator bool(LockAsset<T> locks)
        {
            return locks.HasAny();
        }

        #endregion


        #region Setup

        [CallbackOnEnterEditMode]
        private void OnEnterEditMode()
        {
            if (logLeaks && _locks.Count > 0)
            {
                Debug.LogWarning("Lock Asset!",
                    $"Leak detected in provider collection: {name}\n{_locks.ToCollectionString()}", this);
            }

            if (clearLeaks && _locks.Count > 0)
            {
                ReleaseAll();
            }
        }

        #endregion
    }

    public class LockAsset : MediatorAsset, ILock
    {
        #region Inspector

        [Foldout("Options")]
        [Tooltip("When enabled, leaks that occur when exiting playmode will logged to the console")]
        [SerializeField] private bool logLeaks = true;
        [Tooltip("When enabled, leaks that occur when exiting playmode will be cleared automatically")]
        [SerializeField] private bool clearLeaks = true;

        [ReadOnly]
        [ShowInInspector]
        private HashSet<object> Locks => _lock.Locks;

        private readonly Lock _lock = new();

        #endregion


        #region Public

        /// <summary>
        ///     Event is invoked when the first instance is added.
        /// </summary>
        [PublicAPI]
        public event Action FirstAdded
        {
            add => _lock.FirstAdded += value;
            remove => _lock.FirstAdded -= value;
        }

        /// <summary>
        ///     Event is invoked when all instances were removed.
        /// </summary>
        [PublicAPI]
        public event Action LastRemoved
        {
            add => _lock.LastRemoved += value;
            remove => _lock.LastRemoved -= value;
        }

        /// <summary>
        ///     Returns true if any object is currently registered.
        /// </summary>
        [PublicAPI]
        public bool HasAny()
        {
            return _lock.HasAny();
        }

        /// <summary>
        ///     Returns true if the passed object is registered.
        /// </summary>
        [PublicAPI]
        public bool IsObjectRegistered(object instance)
        {
            return _lock.IsObjectRegistered(instance);
        }

        /// <summary>
        ///     AddSingleton a new object to the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was added, false if it was already added</returns>
        [PublicAPI]
        public bool Add(object instance)
        {
            return _lock.Add(instance);
        }

        /// <summary>
        ///     Remove an object from the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was removed, false if it was not an active lock</returns>
        [PublicAPI]
        public bool Remove(object instance)
        {
            return _lock.Add(instance);
        }

        /// <summary>
        ///     Remove all providing objects and release the lock.
        /// </summary>
        /// <param name="discrete">When true, no events are raised.</param>
        /// <returns>the amount of removed provider</returns>
        [PublicAPI]
        public int ReleaseAll(bool discrete = false)
        {
            return _lock.ReleaseAll(discrete);
        }

        #endregion


        #region Operator

        public static explicit operator bool(LockAsset lockAsset)
        {
            return lockAsset.HasAny();
        }

        #endregion


        #region Setup

        [CallbackOnEnterEditMode]
        private void OnEnterEditMode()
        {
            if (logLeaks && Locks.Count > 0)
            {
                Debug.LogWarning("Lock Asset!",
                    $"Leak detected in provider collection: {name}\n{Locks.ToCollectionString()}", this);
            }

            if (clearLeaks && Locks.Count > 0)
            {
                ReleaseAll();
            }
        }

        #endregion
    }
}