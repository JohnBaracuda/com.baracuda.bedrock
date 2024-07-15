using Baracuda.Utilities.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Baracuda.Bedrock.Scenes
{
    public static class SceneUtilityAPI
    {
        [PublicAPI]
        public static int LastActiveScene { get; private set; }

        [PublicAPI]
        public static int ActiveScene { get; private set; }

        [PublicAPI]
        public static LimitedQueue<int> ActiveSceneHistory { get; } = new(12);

        [PublicAPI]
        public static string GetSceneNameByIndex(int buildIndex)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            var slash = path.LastIndexOf('/');
            var name = path[(slash + 1)..];
            var dot = name.LastIndexOf('.');
            return name[..dot];
        }

        [PublicAPI]
        public static int GetSceneIndexByName(string name)
        {
            for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                if (GetSceneNameByIndex(index) == name)
                {
                    return index;
                }
            }
            Debug.LogError(name);
            return -1;
        }

        [PublicAPI]
        public static string GetSceneNameByPath(string path)
        {
            var buildIndex = SceneUtility.GetBuildIndexByScenePath(path);
            var name = GetSceneNameByIndex(buildIndex);
            return name;
        }

        [PublicAPI]
        public static bool IsSceneLoaded(int sceneIndex)
        {
            for (var loadedSceneIndex = 0; loadedSceneIndex < SceneManager.loadedSceneCount; loadedSceneIndex++)
            {
                if (SceneManager.GetSceneAt(loadedSceneIndex).buildIndex == sceneIndex)
                {
                    return true;
                }
            }
            return false;
        }

        [PublicAPI]
        public static bool IsSceneLoadedAndActive(int sceneIndex)
        {
            return SceneManager.GetActiveScene().buildIndex == sceneIndex;
        }

        [PublicAPI]
        public static Scene GetLoadedSceneByBuildIndex(int sceneIndex)
        {
            for (var loadedSceneIndex = 0; loadedSceneIndex < SceneManager.loadedSceneCount; loadedSceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(loadedSceneIndex);
                if (scene.buildIndex == sceneIndex)
                {
                    return scene;
                }
            }
            return default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            LastActiveScene = default;
            ActiveScene = default;
            ActiveSceneHistory.Clear();
        }

        private static void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            ActiveSceneHistory.Enqueue(scene.buildIndex);
            LastActiveScene = ActiveScene;
            ActiveScene = scene.buildIndex;
        }
    }
}