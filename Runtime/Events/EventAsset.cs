using System;
using System.Collections.Generic;
using Baracuda.Bedrock.Odin;
using Baracuda.Utilities.Events;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace Baracuda.Bedrock.Events
{
    public abstract class EventAsset : EventAssetBase
    {
        private protected readonly Broadcast Event = new();

        public void Add([NotNull] Action listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Add(listener);
        }

        public bool AddUnique([NotNull] Action listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.AddUnique(listener);
        }

        public bool Remove(Action listener)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.Remove(listener);
        }

        public bool Contains(Action listener)
        {
            return Event.Contains(listener);
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Clear()
        {
            Event.Clear();
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void ClearInvalid()
        {
            Event.ClearInvalid();
        }

        [Line]
        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Raise()
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Raise();
        }

        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void RaiseCritical()
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.RaiseCritical();
        }

        public int Count => Event.Count;

        [Line(DrawTiming = DrawTiming.After)]
        [ShowInInspector]
        [Foldout("Debug")]
        [LabelText("Listener")]
        public IReadOnlyCollection<Action> GetListenerCollection => Event.GetListenerCollection;
    }

    public abstract class EventAsset<T> : EventAssetBase
    {
        public event Action<T> Invoked
        {
            add => Add(value);
            remove => Remove(value);
        }

        private protected readonly Broadcast<T> Event = new();

        public void Add([NotNull] Action<T> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Add(listener);
        }

        public bool AddUnique([NotNull] Action<T> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.AddUnique(listener);
        }

        public bool Remove(Action<T> listener)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.Remove(listener);
        }

        public bool Contains(Action<T> listener)
        {
            return Event.Contains(listener);
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Clear()
        {
            Event.Clear();
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void ClearInvalid()
        {
            Event.ClearInvalid();
        }

        [Line]
        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Raise(T value)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Raise(value);
        }

        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void RaiseCritical(T value)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.RaiseCritical(value);
        }

        public int Count => Event.Count;

        [Line(DrawTiming = DrawTiming.After)]
        [ShowInInspector]
        [Foldout("Debug")]
        [LabelText("Listener")]
        public IReadOnlyCollection<Action<T>> GetListenerCollection => Event.GetListenerCollection;
    }

    public abstract class EventAsset<T1, T2> : EventAssetBase
    {
        private protected readonly Broadcast<T1, T2> Event = new();

        public void Add([NotNull] Action<T1, T2> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Add(listener);
        }

        public bool AddUnique([NotNull] Action<T1, T2> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.AddUnique(listener);
        }

        public bool Remove(Action<T1, T2> listener)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.Remove(listener);
        }

        public bool Contains(Action<T1, T2> listener)
        {
            return Event.Contains(listener);
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Clear()
        {
            Event.Clear();
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void ClearInvalid()
        {
            Event.ClearInvalid();
        }

        [Line]
        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Raise(T1 value1, T2 value2)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Raise(value1, value2);
        }

        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void RaiseCritical(T1 value1, T2 value2)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.RaiseCritical(value1, value2);
        }

        public int Count => Event.Count;

        [Line(DrawTiming = DrawTiming.After)]
        [ShowInInspector]
        [Foldout("Debug")]
        [LabelText("Listener")]
        public IReadOnlyCollection<Action<T1, T2>> GetListenerCollection => Event.GetListenerCollection;
    }

    public abstract class EventAsset<T1, T2, T3> : EventAssetBase
    {
        private protected readonly Broadcast<T1, T2, T3> Event = new();

        public void Add([NotNull] Action<T1, T2, T3> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Add(listener);
        }

        public bool AddUnique([NotNull] Action<T1, T2, T3> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.AddUnique(listener);
        }

        public bool Remove(Action<T1, T2, T3> listener)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.Remove(listener);
        }

        public bool Contains(Action<T1, T2, T3> listener)
        {
            return Event.Contains(listener);
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Clear()
        {
            Event.Clear();
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void ClearInvalid()
        {
            Event.ClearInvalid();
        }

        [Line]
        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Raise(T1 value1, T2 value2, T3 value3)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Raise(value1, value2, value3);
        }

        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void RaiseCritical(T1 value1, T2 value2, T3 value3)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.RaiseCritical(value1, value2, value3);
        }

        public int Count => Event.Count;

        [Line(DrawTiming = DrawTiming.After)]
        [ShowInInspector]
        [Foldout("Debug")]
        [LabelText("Listener")]
        public IReadOnlyCollection<Action<T1, T2, T3>> GetListenerCollection => Event.GetListenerCollection;
    }

    public abstract class EventAsset<T1, T2, T3, T4> : EventAssetBase
    {
        private protected readonly Broadcast<T1, T2, T3, T4> Event = new();

        public void Add([NotNull] Action<T1, T2, T3, T4> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Add(listener);
        }

        public bool AddUnique([NotNull] Action<T1, T2, T3, T4> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.AddUnique(listener);
        }

        public bool Remove(Action<T1, T2, T3, T4> listener)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return false;
            }
#endif
            return Event.Remove(listener);
        }

        public bool Contains(Action<T1, T2, T3, T4> listener)
        {
            return Event.Contains(listener);
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Clear()
        {
            Event.Clear();
        }

        [Foldout("Debug")]
        [ButtonGroup("Debug/Clear")]
        [Button(ButtonStyle.FoldoutButton)]
        public void ClearInvalid()
        {
            Event.ClearInvalid();
        }

        [Line]
        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void Raise(T1 value1, T2 value2, T3 value3, T4 value4)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.Raise(value1, value2, value3, value4);
        }

        [Foldout("Debug")]
        [Button(ButtonStyle.FoldoutButton)]
        public void RaiseCritical(T1 value1, T2 value2, T3 value3, T4 value4)
        {
#if UNITY_EDITOR
            if (IsIllegalCall(this))
            {
                return;
            }
#endif
            Event.RaiseCritical(value1, value2, value3, value4);
        }

        public int Count => Event.Count;

        [Line(DrawTiming = DrawTiming.After)]
        [ShowInInspector]
        [Foldout("Debug")]
        [LabelText("Listener")]
        public IReadOnlyCollection<Action<T1, T2, T3, T4>> GetListenerCollection => Event.GetListenerCollection;
    }
}