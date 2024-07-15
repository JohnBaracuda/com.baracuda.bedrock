using System;
using System.Collections.Generic;
using System.Linq;
using Baracuda.Utilities.Events;
using Baracuda.Utilities.Pools;
using JetBrains.Annotations;

namespace Baracuda.Bedrock.Locks
{
    public class Lock : ILock
    {
        #region Fields

        private readonly Broadcast _firstAddedEvent = new();
        private readonly Broadcast _lastRemovedEvent = new();

        #endregion


        #region Public

        /// <summary>
        ///     Hashset containing every lock.
        /// </summary>
        public HashSet<object> Locks { get; } = new(4);

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
        ///     Returns true if any object is currently registered.
        /// </summary>
        [PublicAPI]
        public bool HasAny()
        {
            return Locks.Any();
        }

        /// <summary>
        ///     Returns true if no object is currently registered.
        /// </summary>
        [PublicAPI]
        public bool HasNone()
        {
            return !Locks.Any();
        }

        /// <summary>
        ///     Returns true if the passed object is registered.
        /// </summary>
        [PublicAPI]
        public bool IsObjectRegistered(object instance)
        {
            return Locks.Contains(instance);
        }

        /// <summary>
        ///     AddSingleton a new object to the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was added, false if it was already added</returns>
        [PublicAPI]
        public bool Add(object lockInstance)
        {
            var wasAdded = Locks.Add(lockInstance);
            if (wasAdded && Locks.Count == 1)
            {
                _firstAddedEvent.Raise();
            }
            return wasAdded;
        }

        /// <summary>
        ///     Remove an object from the list of locks. An object can only be added once as a lock!
        /// </summary>
        /// <returns>true if the object was removed, false if it was not an active lock</returns>
        [PublicAPI]
        public bool Remove(object instance)
        {
            var wasRemoved = Locks.Remove(instance);
            if (wasRemoved && Locks.Count == 0)
            {
                _lastRemovedEvent.Raise();
            }
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
            var count = Locks.Count;
            Locks.Clear();
            if (count > 0 && discrete is false)
            {
                _lastRemovedEvent.Raise();
            }
            return count;
        }

        /// <summary>
        ///     Remove all providing objects and remove all callbacks.
        /// </summary>
        [PublicAPI]
        public void Dispose()
        {
            _lastRemovedEvent.Clear();
            _firstAddedEvent.Clear();
            Locks.Clear();
        }

        #endregion


        #region Operator

        public static explicit operator bool(Lock lockAsset)
        {
            return lockAsset.HasAny();
        }

        public override string ToString()
        {
            var sb = StringBuilderPool.Get();
            foreach (var @lock in Locks)
            {
                sb.Append(@lock);
                sb.Append(", ");
            }
            return StringBuilderPool.BuildAndRelease(sb);
        }

        #endregion
    }
}