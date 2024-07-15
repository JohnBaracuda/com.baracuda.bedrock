using System;
using System.Threading;
using Baracuda.Utilities.Types;
using UnityEngine;

namespace Baracuda.Bedrock.PlayerLoop
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadAttribute]
#endif
    public partial class Gameloop
    {
        private static bool EnableApplicationQuit { get; set; }

        static Gameloop()
        {
            Application.wantsToQuit += () =>
            {
                if (EnableApplicationQuit is false)
                {
                    IsQuitting = true;
                    ShutdownAsync();
                }

                return EnableApplicationQuit;
            };

            Application.quitting += () =>
            {
                IsQuitting = true;
                OnQuit();
            };
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
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
#if !DISABLE_GAMELOOP_CALLBACKS
            monoBehaviour = RuntimeMonoBehaviourEvents.Create(
                OnStart,
                OnUpdate,
                OnLateUpdate,
                OnFixedUpdate,
                OnApplicationFocus,
                OnApplicationPause);

            EarlyUpdateEvents.Create(OnPreUpdate, OnPreLateUpdate);
            DelayedUpdateEvents.Create(OnPostUpdate, OnPostLateUpdate);
#endif
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

        private static void OnStart()
        {
#if DEBUG
            for (var index = firstUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    firstUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = firstUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                firstUpdateCallbacks[index]();
            }
#endif
        }

        private static void OnPreUpdate()
        {
            Segment = Segment.PreUpdate;
#if DEBUG
            for (var index = preUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    preUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = preUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                preUpdateCallbacks[index]();
            }
#endif
        }

        private static void OnPostUpdate()
        {
            Segment = Segment.PostUpdate;
#if DEBUG
            for (var index = postUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    postUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = postUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                postUpdateCallbacks[index]();
            }
#endif
        }

        private static void OnPreLateUpdate()
        {
            Segment = Segment.PreLateUpdate;
#if DEBUG
            for (var index = preLateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    preLateUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = preLateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                preLateUpdateCallbacks[index]();
            }
#endif
        }

        private static void OnPostLateUpdate()
        {
            Segment = Segment.PostLateUpdate;
#if DEBUG
            for (var index = postLateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    postLateUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = postLateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                postLateUpdateCallbacks[index]();
            }
#endif
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
            for (var index = slowTickUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    slowTickUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = slowTickUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                slowTickUpdateCallbacks[index]();
            }
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
            for (var index = tickUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    tickUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = tickUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                tickUpdateCallbacks[index]();
            }
#endif
        }

        private static void OnLateUpdate()
        {
            Segment = Segment.LateUpdate;
#if DEBUG
            for (var index = lateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    lateUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = lateUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                lateUpdateCallbacks[index]();
            }
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
            for (var index = fixedUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    fixedUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = fixedUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                fixedUpdateCallbacks[index]();
            }
#endif
            FixedUpdateCount++;
        }

        private static void OnQuit()
        {
            Segment = Segment.ApplicationQuit;
            IsQuitting = true;
            for (var index = applicationQuitCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    applicationQuitCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }

            cancellationTokenSource.Cancel();
        }

        private static void OnApplicationFocus(bool hasFocus)
        {
            Segment = Segment.ApplicationFocus;
#if DEBUG
            for (var index = applicationFocusCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    applicationFocusCallbacks[index](hasFocus);
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = applicationFocusCallbacks.Count - 1; index >= 0; index--)
            {
                applicationFocusCallbacks[index](hasFocus);
            }
#endif
        }

        private static void OnApplicationPause(bool pauseState)
        {
            Segment = Segment.ApplicationPause;
#if DEBUG
            for (var index = applicationPauseCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    applicationPauseCallbacks[index](pauseState);
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = applicationPauseCallbacks.Count - 1; index >= 0; index--)
            {
                applicationPauseCallbacks[index](pauseState);
            }
#endif
        }

        private static void RaiseInitializationCompletedInternal()
        {
            Segment = Segment.InitializationCompleted;
            if (InitializationCompletedState)
            {
                return;
            }

            InitializationCompletedState = true;

#if DEBUG
            for (var index = initializationCompletedCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    initializationCompletedCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = initializationCompletedCallbacks.Count - 1; index >= 0; index--)
            {
                initializationCompletedCallbacks[index]();
            }
#endif
        }

        private static void RaiseCallbackInternal(string callbackName)
        {
            if (!customCallbacks.TryGetValue(callbackName, out var callbacks))
            {
                return;
            }
#if DEBUG
            for (var index = callbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    callbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = callbacks.Count - 1; index >= 0; index--)
            {
                callbacks[index]();
            }
#endif
        }

#if UNITY_EDITOR

        private static void OnEditorUpdate()
        {
            if (Application.isPlaying is false)
            {
                Segment = Segment.EditorUpdate;
            }
#if DEBUG
            for (var index = editorUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    editorUpdateCallbacks[index]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(logCategory, exception);
                }
            }
#else
            for (var index = editorUpdateCallbacks.Count - 1; index >= 0; index--)
            {
                editorUpdateCallbacks[index]();
            }
#endif
        }

        private static void EditorApplicationOnplayModeStateChanged(UnityEditor.PlayModeStateChange state)
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
            for (var i = 0; i < exitPlayModeDelegate.Count; i++)
            {
                if (exitPlayModeDelegate[i] == null)
                {
                    continue;
                }

                try
                {
                    exitPlayModeDelegate[i]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void OnEnterPlayMode()
        {
            for (var i = 0; i < enterPlayModeDelegate.Count; i++)
            {
                if (enterPlayModeDelegate[i] == null)
                {
                    continue;
                }

                try
                {
                    enterPlayModeDelegate[i]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void OnExitEditMode()
        {
            for (var i = 0; i < exitEditModeDelegate.Count; i++)
            {
                if (exitEditModeDelegate[i] == null)
                {
                    continue;
                }

                try
                {
                    exitEditModeDelegate[i]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            FixedUpdateCount = 0;
        }

        private static void OnEnterEditMode()
        {
            IsQuitting = false;
            BeforeSceneLoadCompleted = false;
            AfterSceneLoadCompleted = false;
            InitializationCompletedState = false;

            for (var i = 0; i < enterEditModeDelegate.Count; i++)
            {
                if (enterEditModeDelegate[i] == null)
                {
                    continue;
                }

                try
                {
                    enterEditModeDelegate[i]();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            FixedUpdateCount = 0;
        }

        internal static void RaiseBuildReportPreprocessor(BuildReportData reportData)
        {
            for (var index = buildPreprocessorCallbacks.Count - 1; index >= 0; index--)
            {
                buildPreprocessorCallbacks[index](reportData);
            }
        }

        internal static void RaiseBuildReportPostprocessor(BuildReportData reportData)
        {
            for (var index = buildPostprocessorCallbacks.Count - 1; index >= 0; index--)
            {
                buildPostprocessorCallbacks[index](reportData);
            }
        }
#endif
    }
}