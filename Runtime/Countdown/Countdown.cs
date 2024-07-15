using System;
using System.Collections.Generic;
using Baracuda.Utilities.Events;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Baracuda.Bedrock.Countdown
{
    public class Countdown : ICountdown, IDisposable
    {
        #region Properties

        public float TotalDurationInSeconds => GetTotalDurationInSeconds();
        public float Value { get; set; }
        public float RemainingDurationInSeconds { get; private set; }

        public float PassedDurationInSeconds => TotalDurationInSeconds - RemainingDurationInSeconds;
        public float PercentageCompleted => FactorCompleted * 100;
        public float FactorCompleted => PassedDurationInSeconds * _reciprocalOfTotalDuration;
        public bool IsRunning { get; private set; }
        public bool IsNotRunning => IsRunning is false;
        public bool IsPaused => IsActive && IsRunning is false;
        public bool IsActive { get; private set; }
        public bool IsInactive => IsActive is false;

        public IList<ICountdownDurationModifier> CooldownDurationModifiers { get; } =
            new List<ICountdownDurationModifier>();

        #endregion


        #region Events

        public event Action Completed
        {
            add => _completed.Add(value);
            remove => _completed.Remove(value);
        }

        public event Action Cancelled
        {
            add => _cancelled.Add(value);
            remove => _cancelled.Remove(value);
        }

        public event Action Started
        {
            add => _started.Add(value);
            remove => _started.Remove(value);
        }

        public event Action Restarted
        {
            add => _restarted.Add(value);
            remove => _restarted.Remove(value);
        }

        public event Action Paused
        {
            add => _paused.Add(value);
            remove => _paused.Remove(value);
        }

        public event Action Resumed
        {
            add => _resumed.Add(value);
            remove => _resumed.Remove(value);
        }

        public event Action<float> Reduced
        {
            add => _reduced.Add(value);
            remove => _reduced.Remove(value);
        }

        public event Action Updated
        {
            add => _updated.Add(value);
            remove => _updated.Remove(value);
        }

        #endregion


        #region Fields

        private readonly Broadcast _completed = new();
        private readonly Broadcast _cancelled = new();
        private readonly Broadcast _started = new();
        private readonly Broadcast _restarted = new();
        private readonly Broadcast _paused = new();
        private readonly Broadcast _resumed = new();
        private readonly Broadcast<float> _reduced = new();
        private readonly Broadcast _updated = new();
        private float _reciprocalOfTotalDuration;
        private static readonly IObjectPool<Countdown> pool = new ObjectPool<Countdown>(() => new Countdown());

        #endregion


        #region Factory

        /// <summary>
        ///     Get a new cooldown from a pool of cooldown objects.
        /// </summary>
        /// <param name="durationInSeconds">The duration of the cooldown</param>
        [PublicAPI]
        public static Countdown Create(float durationInSeconds)
        {
            var cooldown = pool.Get();
            cooldown.Value = durationInSeconds;
            return cooldown;
        }

        private Countdown()
        {
        }

        public Countdown(float durationInSeconds) : this()
        {
            Value = durationInSeconds;
        }

        #endregion


        #region Public API

        public bool Start()
        {
            if (IsActive)
            {
                return false;
            }

            CountdownSystem.AddCountdown(this);
            IsRunning = true;
            IsActive = true;
            var totalDurationInSeconds = TotalDurationInSeconds;
            RemainingDurationInSeconds = totalDurationInSeconds;
            _reciprocalOfTotalDuration = 1 / totalDurationInSeconds;
            _started.Raise();
            return true;
        }

        public bool Cancel()
        {
            if (IsInactive)
            {
                return false;
            }

            IsActive = false;
            IsRunning = false;
            RemainingDurationInSeconds = 0;
            CountdownSystem.RemoveCountdown(this);
            _cancelled.Raise();
            return true;
        }

        public bool Complete()
        {
            if (IsInactive)
            {
                return false;
            }

            IsActive = false;
            IsRunning = false;
            RemainingDurationInSeconds = 0;
            CountdownSystem.RemoveCountdown(this);

            _completed.Raise();
            return true;
        }

        public bool Restart(bool startIfInactive = true)
        {
            if (IsInactive)
            {
                if (startIfInactive)
                {
                    Start();
                    return true;
                }
                return false;
            }

            var totalDurationInSeconds = TotalDurationInSeconds;
            RemainingDurationInSeconds = totalDurationInSeconds;
            _reciprocalOfTotalDuration = 1 / totalDurationInSeconds;
            IsRunning = true;
            _restarted.Raise();
            return true;
        }

        public bool Reduce(float durationInSeconds)
        {
            if (IsInactive)
            {
                return false;
            }

            RemainingDurationInSeconds -= durationInSeconds;
            if (RemainingDurationInSeconds <= 0)
            {
                Complete();
            }
            _reduced.Raise(durationInSeconds);
            return true;
        }

        public bool Pause()
        {
            if (IsInactive)
            {
                return false;
            }
            if (IsPaused)
            {
                return false;
            }

            IsRunning = false;
            _paused.Raise();
            return true;
        }

        public bool Resume()
        {
            if (IsInactive)
            {
                return false;
            }
            if (IsRunning)
            {
                return false;
            }

            IsRunning = true;
            _resumed.Raise();
            return true;
        }

        #endregion


        #region Internal

        public void Dispose()
        {
            _started.Clear();
            _completed.Clear();
            _cancelled.Clear();
            _restarted.Clear();
            _reduced.Clear();
            Cancel();
            pool.Release(this);
        }

        private float GetTotalDurationInSeconds()
        {
            var duration = Value;
            foreach (var cooldownDurationModifier in CooldownDurationModifiers)
            {
                cooldownDurationModifier.ModifyCooldownDuration(ref duration, Value);
            }
            return duration;
        }

        void ICountdown.UpdateCountdown(float deltaTime)
        {
            Assert.IsTrue(IsActive);

            if (IsRunning)
            {
                RemainingDurationInSeconds -= deltaTime;
                _updated.Raise();
                if (RemainingDurationInSeconds <= 0)
                {
                    Complete();
                }
            }
        }

        #endregion
    }
}