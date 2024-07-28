using Baracuda.Bedrock.Assets;
using Baracuda.Bedrock.Odin;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Bedrock.Scenes;
using Baracuda.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Baracuda.Bedrock.Initialization
{
    public class Bootstrap : MonoBehaviour
    {
        #region Fields

        [SerializeField] private SceneReference firstLevel;

        [Header("File System")]
        [Preserve]
        [InlineInspector]
        [SerializeField] private FileSystemArgumentsAsset fileSystemArgumentsRelease;
        [Preserve]
        [InlineInspector]
        [SerializeField] private FileSystemArgumentsAsset fileSystemArgumentsDebug;
        [Preserve]
        [InlineInspector]
        [SerializeField] private FileSystemArgumentsAsset fileSystemArgumentsEditor;
        [Space]
        [SerializeField] private string bootstrapKey = "Preload";

        private const string GameInstallerAssetName = "GameInstaller";

        #endregion


        #region Initialization

        private void Awake()
        {
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            Debug.Log(nameof(Bootstrap), "Initialization Started");

#if UNITY_EDITOR
            await Resources.UnloadUnusedAssets();
#endif

            Debug.Log(nameof(Bootstrap), "(1/6) Initializing File System");
#if UNITY_EDITOR
            if (FileSystem.IsInitialized)
            {
                var args = new FileSystemShutdownArgs
                {
                    forceSynchronousShutdown = true
                };

                await FileSystem.ShutdownAsync(args);
            }
#endif

            var arguments =
#if UNITY_EDITOR
                fileSystemArgumentsEditor;
#elif DEBUG
                fileSystemArgumentsDebug;
#else
                fileSystemArgumentsEditor;
#endif

            await FileSystem.InitializeAsync(arguments);

            Debug.Log(nameof(Bootstrap), "(2/6) Loading Addressables");

            await Addressables.InitializeAsync();

            var locations = await Addressables.LoadResourceLocationsAsync(bootstrapKey);

            foreach (var resourceLocation in locations)
            {
                Debug.Log(nameof(Bootstrap), $"Loading Resource [{resourceLocation.PrimaryKey}]");

                await Addressables.LoadAssetAsync<Object>(resourceLocation);
            }

            Debug.Log(nameof(Bootstrap), "(3/6) Loading Resources");

            var services = (InstallationAsset)await Resources.LoadAsync<InstallationAsset>(GameInstallerAssetName);

            Debug.Log(nameof(Bootstrap), "(4/6) Installing Domain Services");

            services.InstallDomainServices();

            Debug.Log(nameof(Bootstrap), "(5/6) Installing Runtime Services");

            services.InstallRuntimeServices();

            Gameloop.RaiseInitializationCompleted();

            Debug.Log(nameof(Bootstrap), "(6/6) Loading First Level");

            await SceneLoader
                .Create()
                .ScheduleScene(firstLevel)
                .AsBlocking()
                .LoadAsync();

            Debug.Log(nameof(Bootstrap), "Initialization Completed");
        }

        #endregion


        #region Debug & Editor

#if UNITY_EDITOR

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            var activeScene = SceneManager.GetActiveScene();
            var sceneIndex = SceneUtility.GetBuildIndexByScenePath(activeScene.path);
            var isInitializationScene = sceneIndex == 0;

            if (isInitializationScene)
            {
                return;
            }

            Debug.Log("Bootstrap", "Initializing Editor Systems");

            if (FileSystem.IsInitialized is false)
            {
                Debug.LogError("Please make sure to launch the game with an initialized File System int the Editor!");
            }

            Debug.Log("Bootstrap", "Installing Runtime Systems");
            var services = Resources.Load<InstallationAsset>(GameInstallerAssetName);
            services.InstallDomainServices();
            services.InstallRuntimeServices();
            Gameloop.RaiseInitializationCompleted();
        }

        [UnityEditor.InitializeOnLoadMethodAttribute]
        private static void OnLoad()
        {
            var services = Resources.Load<InstallationAsset>(GameInstallerAssetName);
            if (services)
            {
                services.InstallDomainServices();
            }
        }
#endif

        #endregion
    }
}