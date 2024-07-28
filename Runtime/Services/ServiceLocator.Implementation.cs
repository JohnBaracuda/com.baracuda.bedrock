using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Baracuda.Bedrock.Services
{
    public sealed partial class ServiceLocator : MonoBehaviour
    {
        #region Static Fields

        private static ServiceLocator runtimeServiceLocator;
        private static readonly ServiceContainer domainServices = new();
        private static readonly ServiceContainer editorServices = new();
        private static Dictionary<Scene, ServiceLocator> SceneLocators { get; } = new();
        private static Dictionary<string, ServiceLocator> ScopedLocators { get; } = new();

        private const string GlobalServiceLocatorName = "ServiceLocator [Global]";
        private const string SceneServiceLocatorName = "ServiceLocator [ForScene]";

        #endregion


        #region Fields

        private readonly ServiceContainer _container = new();

        #endregion


        #region Internal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer GetRuntimeContainerInternal()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return Domain;
            }
#endif

            if (runtimeServiceLocator != null)
            {
                return runtimeServiceLocator._container;
            }

#if UNITY_EDITOR
            if (Gameloop.IsQuitting)
            {
                return null;
            }
#endif

            var containerGameObject = new GameObject(GlobalServiceLocatorName);
            var serviceLocator = containerGameObject.AddComponent<ServiceLocator>();
            containerGameObject.DontDestroyOnLoad();
            var container = serviceLocator.Container();
            runtimeServiceLocator = serviceLocator;
            container.WithFallbackContainer(domainServices);
            return container;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer GetDomainContainerInternal()
        {
            return domainServices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer GetEditorContainerInternal()
        {
            return editorServices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer ForActiveSceneInternal(bool create)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Application must be running!");
            }
#endif

            var scene = SceneManager.GetActiveScene();

            return ForScene(scene, create);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer ForSceneOfInternal(MonoBehaviour monoBehaviour, bool create)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Application must be running!");
            }
#endif
            var scene = monoBehaviour.gameObject.scene;

            if (SceneLocators.TryGetValue(scene, out var locator))
            {
                return locator._container;
            }
            return create ? CreateSceneLocator(scene)._container : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer ForSceneInternal(Scene scene, bool create)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Application must be running!");
            }
#endif
            if (SceneLocators.TryGetValue(scene, out var locator))
            {
                return locator._container;
            }
            return create ? CreateSceneLocator(scene)._container : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContainer ForScopeInternal(string scope)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Application must be running!");
            }
#endif
            if (ScopedLocators.TryGetValue(scope, out var locator))
            {
                return locator._container;
            }

            var gameObject = new GameObject($"ServiceLocator [{scope}]");
            gameObject.DontDestroyOnLoad();
            var serviceLocator = gameObject.AddComponent<ServiceLocator>();
            ScopedLocators.Add(scope, serviceLocator);

            return serviceLocator._container;
        }

        #endregion


        #region Setup

        private void OnDestroy()
        {
            if (this == runtimeServiceLocator)
            {
                runtimeServiceLocator = null;
            }
            else if (SceneLocators.ContainsValue(this))
            {
                SceneLocators.Remove(gameObject.scene);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runtimeServiceLocator = null;
            SceneLocators.Clear();
            ScopedLocators.Clear();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.isSubScene)
            {
                return;
            }
            if (ForScene(scene) is not null)
            {
                return;
            }

            CreateSceneLocator(scene);
        }

        private static ServiceLocator CreateSceneLocator(Scene scene)
        {
            var gameObject = new GameObject(SceneServiceLocatorName);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var sceneLocator = gameObject.AddComponent<ServiceLocator>();
            SceneLocators.Add(scene, sceneLocator);
            return sceneLocator;
        }

        #endregion
    }
}