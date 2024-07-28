using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Baracuda.Bedrock.Services
{
    [DisallowMultipleComponent]
    public sealed partial class ServiceLocator : MonoBehaviour
    {
        #region Service Locator

        /// <summary>
        ///     Get the service container that holds all runtime services.
        ///     This includes runtime managers and systems.
        /// </summary>
        [PublicAPI]
        public static ServiceContainer Runtime => GetRuntimeContainerInternal();

        /// <summary>
        ///     Get the service container that holds all domain services.
        ///     This includes settings and systems that are available both during runtime and in the editor.
        /// </summary>
        [PublicAPI]
        public static ServiceContainer Domain => GetDomainContainerInternal();

        /// <summary>
        ///     Get the service container that holds all editor services.
        ///     This includes settings and systems that are only available in the editor.
        /// </summary>
        [PublicAPI]
        public static ServiceContainer Editor => GetEditorContainerInternal();

        /// <summary>
        ///     Get the <see cref="ServiceContainer" /> for the active scene. This call will create a new
        ///     ServiceContainer for the active scene if none is found. Use <see cref="ForActiveSceneOrNull" />
        ///     if you try to get the service container
        ///     instead.
        /// </summary>
        [PublicAPI]
        public static ServiceContainer ForActiveScene => ForActiveSceneInternal(true);

        /// <summary>
        ///     Get the <see cref="ServiceContainer" /> for the active scene or null. This call will not create a new
        ///     ServiceContainer for the active scene if none is found. Use <see cref="ForActiveScene" />
        ///     if you want to ensure that a new service container is created if none is found.
        /// </summary>
        [PublicAPI]
        [CanBeNull]
        public static ServiceContainer ForActiveSceneOrNull => ForActiveSceneInternal(false);

        [PublicAPI]
        [CanBeNull]
        public static ServiceContainer ForSceneOf(MonoBehaviour monoBehaviour, bool create = false)
        {
            return ForSceneOfInternal(monoBehaviour, create);
        }

        [PublicAPI]
        [CanBeNull]
        public static ServiceContainer ForScene(Scene scene, bool create = false)
        {
            return ForSceneInternal(scene, create);
        }

        [PublicAPI]
        public static ServiceContainer ForScope(string scope)
        {
            return ForScopeInternal(scope);
        }

        [PublicAPI]
        public ServiceContainer Container()
        {
            return _container;
        }

        #endregion


        #region Container

        [PublicAPI]
        public static async UniTask<T> GetAsync<T>() where T : class
        {
            while (true)
            {
                if (TryGet<T>(out var result))
                {
                    return result;
                }
                await UniTask.NextFrame();
            }
        }

        [PublicAPI]
        public static T Get<T>() where T : class
        {
            return Runtime.Get<T>();
        }

        [PublicAPI]
        public static void Inject<T>(ref T field) where T : class
        {
            field = Runtime.Get<T>();
        }

        [PublicAPI]
        public static object Get(Type type)
        {
            return Runtime.Get(type);
        }

        [PublicAPI]
        public static bool TryGet<T>(out T service) where T : class
        {
            return Runtime.TryGet(out service);
        }

        [PublicAPI]
        public static bool TryGet(Type type, out object value)
        {
            return Runtime.TryGet(type, out value);
        }

        [PublicAPI]
        public static void AddSingleton<T>(T service)
        {
            Runtime?.Add(service);
        }

        [PublicAPI]
        public static void AddSingleton(Type type, object service)
        {
            Runtime?.Add(type, service);
        }

        [PublicAPI]
        public static void AddTransient<T>(Func<T> serviceFunc)
        {
            Runtime?.AddTransient(serviceFunc);
        }

        [PublicAPI]
        public static void AddTransient(Type type, Delegate serviceProvider)
        {
            Runtime?.AddTransient(type, serviceProvider);
        }

        [PublicAPI]
        public static void AddLazy<T>(Func<T> serviceFunc)
        {
            Runtime?.AddLazy(serviceFunc);
        }

        [PublicAPI]
        public static void AddLazy(Type type, Delegate serviceProvider)
        {
            Runtime?.AddLazy(type, serviceProvider);
        }

        [PublicAPI]
        public static void Remove<T>(T service)
        {
            Runtime?.Remove(service);
        }

        [PublicAPI]
        public static void Remove<T>()
        {
            Runtime?.Remove<T>();
        }

        #endregion
    }
}