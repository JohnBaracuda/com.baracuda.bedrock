using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Baracuda.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baracuda.Bedrock.PlayerLoop
{
    /// <summary>
    ///     Class handles custom gameloop specific callbacks as well as other gameloop relevant data.
    ///     This includes time scale, frame count, editor state, initialization callbacks and more.
    /// </summary>
    public partial class Gameloop
    {
        #region Callbacks

        /// <summary>
        ///     AddSingleton an object making it receive custom callbacks.
        /// </summary>
        /// <param name="target">The object to register</param>
        public static void Register(Object target)
        {
#if !DISABLE_GAMELOOP_CALLBACKS
            RegisterInternal(target);
#endif
        }

        /// <summary>
        ///     Unregister an object making it no longer receive custom callbacks.
        /// </summary>
        /// <param name="target">The object to unregister</param>
        public static void Unregister(Object target)
        {
#if !DISABLE_GAMELOOP_CALLBACKS
            UnregisterInternal(target);
#endif
        }

        /// <summary>
        ///     Inject a custom callback method.
        /// </summary>
        /// <param name="callback">The name of the callback method</param>
        public static void RaiseCallback(string callback)
        {
            RaiseCallbackInternal(callback);
        }

        /// <summary>
        ///     Inject the initialization completed callback.
        /// </summary>
        public static void RaiseInitializationCompleted()
        {
            Debug.Log("Gameloop", "Raising initialization completed!");
            RaiseCallback("RaiseInitializationCompletedFirstPass");
            RaiseInitializationCompletedInternal();
        }

        /// <summary>
        ///     Begin an asynchronous shutdown process.
        /// </summary>
        public static void Shutdown()
        {
            ShutdownAsync();
        }

        /// <summary>
        ///     Begin an asynchronous shutdown process.
        /// </summary>
        public static Task ShutdownAsync()
        {
            return ShutdownAsyncInternal();
        }

        #endregion


        #region State

        /// <summary>
        ///     Returns true if the application is not quitting.
        /// </summary>
        public static bool IsRunning => Application.isPlaying && !IsQuitting;

        /// <summary>
        ///     Returns true if the application is not running.
        /// </summary>
        public static bool IsNotRunning => !IsRunning;

        /// <summary>
        ///     Returns true if the application quitting process has started.
        /// </summary>
        public static bool IsQuitting { get; private set; }

        /// <summary>
        ///     The editor play mode state. Will only return a valid value in editor.
        /// </summary>
        public static int EditorState { get; private set; }

        /// <summary>
        ///     Returns true if <see cref="RaiseInitializationCompleted" /> was raised.
        /// </summary>
        public static bool InitializationCompletedState { get; private set; }

        /// <summary>
        ///     Returns true if the before scene load callback was raised.
        /// </summary>
        public static bool BeforeSceneLoadCompleted { get; private set; }

        /// <summary>
        ///     Returns true if the after scene load callback was raised.
        /// </summary>
        public static bool AfterSceneLoadCompleted { get; private set; }

        /// <summary>
        ///     Get the current gameloop segment. (Update, LateUpdate etc.)
        /// </summary>
        public static Segment Segment { get; private set; } = Segment.None;

        /// <summary>
        ///     Get the current physics update count.
        /// </summary>
        public static int FixedUpdateCount { get; private set; }

        /// <summary>
        ///     Return a <see cref="CancellationToken" /> that is valid for the duration of the applications runtime.
        ///     This means until OnApplicationQuit is called in a build
        ///     or until the play state is changed in the editor.
        /// </summary>
        public static CancellationToken RuntimeToken => cancellationTokenSource.Token;

        #endregion


        #region Events

        /// <summary>
        ///     Called during shutdown for async shutdown logic.
        /// </summary>
        public static event Func<Task> AsyncShutdown
        {
            add => asyncShutdownCallbacks.Add(value);
            remove => asyncShutdownCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called before the first scene is loaded.
        ///     This event is called retroactively.
        /// </summary>
        public static event Action BeforeFirstSceneLoad
        {
            add => beforeFirstSceneLoadCallbacks.AddNullChecked(value);
            remove => beforeFirstSceneLoadCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called after the first scene is loaded.
        ///     This event is called retroactively.
        /// </summary>
        public static event Action AfterFirstSceneLoad
        {
            add => afterFirstSceneLoadCallbacks.AddNullChecked(value);
            remove => afterFirstSceneLoadCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called during shutdown.
        /// </summary>
        public static event Action ApplicationQuit
        {
            add => applicationQuitCallbacks.AddNullChecked(value);
            remove => applicationQuitCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called when the focus of the application was changed.
        /// </summary>
        public static event Action<bool> ApplicationFocusChanged
        {
            add => applicationFocusCallbacks.AddNullChecked(value);
            remove => applicationFocusCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called when pause state of the application was changed.
        /// </summary>
        public static event Action<bool> ApplicationPauseChanged
        {
            add => applicationPauseCallbacks.AddNullChecked(value);
            remove => applicationPauseCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called every frame.
        /// </summary>
        public static event Action Update
        {
            add => updateCallbacks.AddNullChecked(value);
            remove => updateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called every frame during late update.
        /// </summary>
        public static event Action LateUpdate
        {
            add => lateUpdateCallbacks.AddNullChecked(value);
            remove => lateUpdateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called every frame during late update.
        /// </summary>
        public static event Action PostLateUpdate
        {
            add => postLateUpdateCallbacks.AddNullChecked(value);
            remove => postLateUpdateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called every physics update.
        /// </summary>
        public static event Action FixedUpdate
        {
            add => fixedUpdateCallbacks.AddNullChecked(value);
            remove => fixedUpdateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called once every second.
        /// </summary>
        public static event Action SlowTick
        {
            add => slowTickUpdateCallbacks.AddNullChecked(value);
            remove => slowTickUpdateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called 10 times per second.
        /// </summary>
        public static event Action Tick
        {
            add => tickUpdateCallbacks.AddNullChecked(value);
            remove => tickUpdateCallbacks.Remove(value);
        }

        /// <summary>
        ///     Called when asynchronous initialization has completed.
        ///     Requires <see cref="RaiseInitializationCompleted" /> to be called.
        ///     This event is called retroactively.
        /// </summary>
        public static event Action InitializationCompleted
        {
            add => initializationCompletedCallbacks.AddNullChecked(value);
            remove => initializationCompletedCallbacks.Remove(value);
        }

        #endregion


        #region Time Scale

        /// <summary>
        ///     When enabled, TimeScaleDelegates can be added to control the time scale of the game.
        /// </summary>
        public static bool ControlTimeScale { get; set; } = true;

        /// <summary>
        ///     AddSingleton a custom timescale modification source.
        /// </summary>
        public static void AddTimeScaleModifier(TimeScaleDelegate modifier)
        {
            timeScaleModifier.AddUnique(modifier);
        }

        /// <summary>
        ///     Remove a custom timescale modification source.
        /// </summary>
        public static void RemoveTimeScaleModifier(TimeScaleDelegate modifier)
        {
            timeScaleModifier.Remove(modifier);
        }

        /// <summary>
        ///     Removes all active time scale modification sources.
        /// </summary>
        public static void ClearTimeScaleModifier()
        {
            timeScaleModifier.Clear();
        }

        #endregion


        #region Coroutines

        public static Coroutine StartCoroutine(IEnumerator enumerator)
        {
            if (monoBehaviour == null)
            {
                return null;
            }

            return monoBehaviour.StartCoroutine(enumerator);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            if (monoBehaviour == null)
            {
                return;
            }

            monoBehaviour.StopCoroutine(coroutine);
        }

        #endregion


        #region Validations

        // ReSharper disable Unity.PerformanceAnalysis
        public static bool IsDelegateSubscribedToUpdate(Action update)
        {
            return updateCallbacks.Contains(update);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static bool IsDelegateSubscribedToLateUpdate(Action update)
        {
            return lateUpdateCallbacks.Contains(update);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static bool IsDelegateSubscribedToFixedUpdate(Action update)
        {
            return fixedUpdateCallbacks.Contains(update);
        }

        #endregion


        #region Editor

        public static float TickDelta => TickScaledTimer.Delta();

        public static async Task DelayedCallAsync()
        {
#if UNITY_EDITOR
            var completionSource = new TaskCompletionSource<object>();
            UnityEditor.EditorApplication.delayCall += () => { completionSource.SetResult(null); };
            await completionSource.Task;
#else
            await Task.CompletedTask;
#endif
        }

        #endregion
    }
}