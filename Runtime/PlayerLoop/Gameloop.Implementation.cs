﻿using System;
using System.Collections.Generic;
using System.Threading;
using Baracuda.Utility.Timing;
using Baracuda.Utility.Types;
using UnityEngine;

namespace Baracuda.Utility.PlayerLoop
{
    public partial class Gameloop
    {
        #region Fields

        private const int Capacity4 = 4;
        private const int Capacity8 = 8;
        private const int Capacity16 = 16;
        private const int Capacity32 = 32;
        private const int Capacity64 = 64;
        private const int Capacity128 = 128;
        private const int Capacity256 = 256;

        private static readonly LogCategory logCategory = nameof(Gameloop);

        private static readonly Broadcast initializationCompletedCallbacks = new(Capacity64);
        private static readonly Broadcast beforeFirstSceneLoadCallbacks = new(Capacity16);
        private static readonly Broadcast afterFirstSceneLoadCallbacks = new(Capacity16);

        private static readonly Broadcast updateCallbacks = new(Capacity64);
        private static readonly Broadcast lateUpdateCallbacks = new(Capacity32);
        private static readonly Broadcast fixedUpdateCallbacks = new(Capacity32);

        private static readonly Broadcast slowTickUpdateCallbacks = new(Capacity32);
        private static readonly Broadcast tickUpdateCallbacks = new(Capacity32);
        private static readonly Broadcast applicationQuitCallbacks = new(Capacity32);
        private static readonly Broadcast<bool> applicationFocusCallbacks = new(Capacity16);
        private static readonly Broadcast<bool> applicationPauseCallbacks = new(Capacity16);
        private static MonoBehaviour monoBehaviour;

        private static CancellationTokenSource cancellationTokenSource = new();

        private static ScaledTimer OneSecondScaledTimer { get; set; } = ScaledTimer.None;
        private static ScaledTimer TickScaledTimer { get; set; }

        #endregion


        #region Timeloop

        public delegate void TimeScaleDelegate(ref float timeScale);

        private static readonly List<TimeScaleDelegate> timeScaleModifier = new();

        private static void UpdateTimeScale()
        {
            if (ControlTimeScale is false)
            {
                return;
            }

            var timeScale = 1f;
            foreach (var timeScaleDelegate in timeScaleModifier)
            {
                timeScaleDelegate(ref timeScale);
            }

            Time.timeScale = timeScale;
        }

        #endregion


        #region Shutdown

        private static void ShutdownInternal()
        {
            Debug.Log("Gameloop", "Quitting Application");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        #endregion


        static Gameloop()
        {
            Application.quitting += () =>
            {
                IsQuitting = true;
                OnQuit();
            };
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            UnityEditor.EditorApplication.projectWindowItemInstanceOnGUI += (_, _) => Segment = Segment.OnGUI;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCancellationTokenSource()
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeFirstSceneLoaded()
        {
            Segment = Segment.BeforeFirstSceneLoad;
            IsQuitting = false;
            monoBehaviour = RuntimeMonoBehaviourEvents.Create(
                OnUpdate,
                OnLateUpdate,
                OnFixedUpdate,
                OnApplicationFocus,
                OnApplicationPause);

            for (var index = beforeFirstSceneLoadCallbacks.Count - 1; index >= 0; index--)
            {
                beforeFirstSceneLoadCallbacks[index]();
            }

            BeforeSceneLoadCompleted = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterFirstSceneLoaded()
        {
            Segment = Segment.AfterFirstSceneLoad;
            for (var index = afterFirstSceneLoadCallbacks.Count - 1; index >= 0; index--)
            {
                afterFirstSceneLoadCallbacks[index]();
            }

            AfterSceneLoadCompleted = true;
        }

        private static void OnUpdate()
        {
            Segment = Segment.Update;
#if DEBUG
            for (var index = updateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    updateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = updateCallbacks.Count - 1; index >= 0; index--)
            {
                updateCallbacks[index]();
            }
#endif
            OnSlowTickUpdate();
            OnTickUpdate();
        }

        private static void OnSlowTickUpdate()
        {
            if (OneSecondScaledTimer.IsRunning)
            {
                return;
            }

            OneSecondScaledTimer = ScaledTimer.FromSeconds(1);
#if DEBUG
            slowTickUpdateCallbacks.RaiseCritical();
#else
            slowTickUpdateCallbacks.Raise();
#endif
        }

        private static void OnTickUpdate()
        {
            if (TickScaledTimer.IsRunning)
            {
                return;
            }

            TickScaledTimer = ScaledTimer.FromSeconds(.1f);
#if DEBUG
            tickUpdateCallbacks.RaiseCritical();
#else
            tickUpdateCallbacks.Raise();
#endif
        }

        private static void OnLateUpdate()
        {
            Segment = Segment.LateUpdate;

#if DEBUG
            lateUpdateCallbacks.RaiseCritical();
#else
            lateUpdateCallbacks.Raise();
#endif

            try
            {
                UpdateTimeScale();
            }
            catch (Exception exception)
            {
                Debug.LogException(logCategory, exception);
            }
        }

        private static void OnFixedUpdate()
        {
            Segment = Segment.FixedUpdate;

#if DEBUG
            fixedUpdateCallbacks.RaiseCritical();
#else
            fixedUpdateCallbacks.Raise();
#endif

            FixedUpdateCount++;
        }

        private static void OnQuit()
        {
            Segment = Segment.ApplicationQuit;
            IsQuitting = true;

            applicationQuitCallbacks.RaiseCritical();
            cancellationTokenSource.Cancel();
        }

        private static void OnApplicationFocus(bool hasFocus)
        {
            Segment = Segment.ApplicationFocus;
            applicationFocusCallbacks.RaiseCritical(hasFocus);
        }

        private static void OnApplicationPause(bool pauseState)
        {
            Segment = Segment.ApplicationPause;
            applicationPauseCallbacks.RaiseCritical(pauseState);
        }

        private static void RaiseInitializationCompletedInternal()
        {
            Segment = Segment.InitializationCompleted;
            if (InitializationCompletedState)
            {
                return;
            }

            InitializationCompletedState = true;
            initializationCompletedCallbacks.RaiseCritical();
        }

#if UNITY_EDITOR
        private static void OnEditorUpdate()
        {
            if (Application.isPlaying is false)
            {
                Segment = Segment.EditorUpdate;
            }
            editorUpdateCallbacks.RaiseCritical();
        }

        private static void EditorApplicationOnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            EditorState = (int)state;
            switch (state)
            {
                case UnityEditor.PlayModeStateChange.EnteredEditMode:
                    OnEnterEditMode();
                    break;

                case UnityEditor.PlayModeStateChange.ExitingEditMode:
                    OnExitEditMode();
                    break;

                case UnityEditor.PlayModeStateChange.EnteredPlayMode:
                    OnEnterPlayMode();
                    break;

                case UnityEditor.PlayModeStateChange.ExitingPlayMode:
                    OnExitPlayMode();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private static void OnExitPlayMode()
        {
            IsQuitting = true;
            exitPlayModeDelegate.RaiseCritical();
        }

        private static void OnEnterPlayMode()
        {
            enterPlayModeDelegate.RaiseCritical();
        }

        private static void OnExitEditMode()
        {
            exitEditModeDelegate.RaiseCritical();

            FixedUpdateCount = 0;
        }

        private static void OnEnterEditMode()
        {
            IsQuitting = false;
            BeforeSceneLoadCompleted = false;
            AfterSceneLoadCompleted = false;
            InitializationCompletedState = false;

            enterEditModeDelegate.RaiseCritical();

            FixedUpdateCount = 0;
        }
#endif
    }
}