using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Baracuda.Bedrock.PlayerLoop;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Baracuda.Bedrock.Scenes
{
    /// <summary>
    ///     This struct can be used to layout and then execute complex scene load operations.
    /// </summary>
    public readonly ref struct SceneLoader
    {
        #region Builder API

        /// <summary>
        ///     High level API to check if a scene load is currently active.
        ///     Note that this only returns true for scenes loaded with the SceneLoader.
        /// </summary>
        [PublicAPI]
        public static bool IsSceneLoadActive { get; private set; }

        /// <summary>
        ///     Use this to validate if the <see cref="SceneLoader" /> has been created or if it is just the default value.
        /// </summary>
        [PublicAPI]
        public readonly bool IsCreated;

        /// <summary>
        ///     Create a new scene loader.
        /// </summary>
        public static SceneLoader Create()
        {
            return new SceneLoader(ListPool<SceneLoadData>.Get());
        }

        /// <summary>
        ///     Schedule the next scene load.
        /// </summary>
        [PublicAPI]
        public SceneEntryBuilder ScheduleScene(SceneBuildIndex buildIndex)
        {
            Assert.IsTrue(IsCreated);
            return new SceneEntryBuilder(this, buildIndex);
        }

        /// <summary>
        ///     Schedule the next scene load.
        /// </summary>
        [PublicAPI]
        public SceneEntryBuilder ScheduleScene(string sceneName)
        {
            Assert.IsTrue(IsCreated);
            return new SceneEntryBuilder(this, SceneUtilityAPI.GetSceneIndexByName(sceneName));
        }

        /// <summary>
        ///     Schedule the next scene load.
        /// </summary>
        [PublicAPI]
        public SceneEntryBuilder ScheduleScene(SceneReference scene)
        {
            Assert.IsTrue(IsCreated);
            return new SceneEntryBuilder(this, scene.BuildIndex);
        }

        /// <summary>
        ///     Begin the scene load asynchronous. This method will return synchronous if every scene was scheduled with
        ///     <see cref="SceneEntryBuilder.AsBlocking" />
        /// </summary>
        [PublicAPI]
        public UniTask LoadAsync()
        {
            Assert.IsTrue(IsCreated);
            return LoadAsyncInternal(_entries);
        }

        #endregion


        #region Scene Entry Builder

        public ref struct SceneEntryBuilder
        {
            /// <summary>
            ///     Set the scene as the main scene.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder AsMain()
            {
                _loadData.IsMainScene = true;
                return this;
            }

            /// <summary>
            ///     Perform the scene load using the passed scene load delegate.
            ///     This can be used for custom network or addressable scene loads.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder WithAsyncSceneLoader(IAsyncSceneLoader loader)
            {
                _loadData.AsyncSceneLoader = loader;
                return this;
            }

            /// <summary>
            ///     Set the scene to be loaded synchronously.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder AsBlocking()
            {
                _loadData.LoadAsync = false;
                return this;
            }

            /// <summary>
            ///     Set the scene to be loaded in parallel with other scenes.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder AsParallel()
            {
                _loadData.LoadParallel = true;
                return this;
            }

            /// <summary>
            ///     Complete the current scene load entry and schedule the next scene.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder ScheduleScene(SceneBuildIndex buildIndex)
            {
                _sceneLoader.Add(_loadData);
                return new SceneEntryBuilder(_sceneLoader, buildIndex);
            }

            /// <summary>
            ///     Complete the current scene load entry and schedule the next scene.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder ScheduleScene(string sceneName)
            {
                _sceneLoader.Add(_loadData);
                return new SceneEntryBuilder(_sceneLoader, SceneUtilityAPI.GetSceneIndexByName(sceneName));
            }

            /// <summary>
            ///     Complete the current scene load entry and schedule the next scene.
            /// </summary>
            [PublicAPI]
            public SceneEntryBuilder ScheduleScene(SceneReference scene)
            {
                _sceneLoader.Add(_loadData);
                return new SceneEntryBuilder(_sceneLoader, scene.BuildIndex);
            }

            /// <summary>
            ///     Build the scene loader without starting it.
            /// </summary>
            [PublicAPI]
            public SceneLoader Build()
            {
                _sceneLoader.Add(_loadData);
                return _sceneLoader;
            }

            /// <summary>
            ///     Build the scene load data and start the scene load.
            /// </summary>
            [PublicAPI]
            public UniTask LoadAsync()
            {
                return Build().LoadAsync();
            }


            #region Fields & Ctor

            private readonly SceneLoader _sceneLoader;
            private SceneLoadData _loadData;

            public SceneEntryBuilder(SceneLoader sceneLoader, SceneBuildIndex buildIndex)
            {
                _sceneLoader = sceneLoader;

                _loadData = new SceneLoadData
                {
                    BuildIndex = buildIndex,
                    LoadAsync = true,
                    IsMainScene = false,
                    LoadParallel = false,
                    ActivateOnLoad = true
                };
            }

            #endregion
        }

        #endregion


        #region Ctor & Data

        private void Add(SceneLoadData loadData)
        {
            _entries.Add(loadData);
        }

        private struct SceneLoadData
        {
            public bool LoadAsync;
            public bool LoadParallel;
            public bool IsMainScene;
            public IAsyncSceneLoader AsyncSceneLoader;
            public bool ActivateOnLoad;
            public SceneBuildIndex BuildIndex;
        }

        private readonly List<SceneLoadData> _entries;

        private SceneLoader(List<SceneLoadData> entries)
        {
            IsCreated = true;
            _entries = entries;
        }

        #endregion


        #region Loading

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async UniTask LoadAsyncInternal(List<SceneLoadData> entries)
        {
            try
            {
                if (IsSceneLoadActive)
                {
                    throw new InvalidOperationException("Another scene Load is already active!");
                }

                IsSceneLoadActive = true;
                var loadSceneMode = LoadSceneMode.Single;

                foreach (var sceneEntry in entries)
                {
                    var buildIndex = sceneEntry.BuildIndex;

                    if (sceneEntry.LoadAsync)
                    {
                        Gameloop.RuntimeToken.ThrowIfCancellationRequested();

                        if (sceneEntry.AsyncSceneLoader is not null)
                        {
                            var scene = await sceneEntry.AsyncSceneLoader.LoadSceneAsync(buildIndex, loadSceneMode);
                            if (sceneEntry.ActivateOnLoad)
                            {
                                SceneManager.SetActiveScene(scene);
                            }
                        }
                        else
                        {
                            var asyncOperation = SceneManager.LoadSceneAsync(buildIndex, loadSceneMode);
                            if (asyncOperation is null)
                            {
                                continue;
                            }

                            asyncOperation.allowSceneActivation = sceneEntry.ActivateOnLoad;
                            await asyncOperation.ToUniTask();
                        }
                    }
                    else
                    {
                        SceneManager.LoadScene(buildIndex, loadSceneMode);
                    }

                    if (sceneEntry.IsMainScene)
                    {
                        var scene = SceneManager.GetSceneByBuildIndex(buildIndex);
                        SceneManager.SetActiveScene(scene);
                    }

                    loadSceneMode = LoadSceneMode.Additive;
                }

                ListPool<SceneLoadData>.Release(entries);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Scene Load", "Scene Load Operation Cancelled");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                IsSceneLoadActive = false;
            }
        }

        #endregion
    }
}