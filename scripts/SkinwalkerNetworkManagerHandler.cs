namespace OptimizedSkinwalkers
{
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class SkinwalkerNetworkManagerHandler
    {
        private static GameObject networkManagerInstance;
        private static bool initializationSuccessful;

        public static bool TryClientConnectInitializer(Scene sceneName)
        {
            initializationSuccessful = false;
            ClientConnectInitializer(sceneName, default);
            return initializationSuccessful;
        }

        public static void ClientConnectInitializer(Scene sceneName, LoadSceneMode _)
        {
            if (sceneName.name == "SampleSceneRelay")
            {
                networkManagerInstance = new GameObject("SkinwalkerNetworkManager");
                networkManagerInstance.AddComponent<NetworkObject>();
                networkManagerInstance.AddComponent<SkinwalkerNetworkManager>();

                networkManagerInstance.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(networkManagerInstance);

                initializationSuccessful = true;
                SkinwalkerLogger.Log("Initialized SkinwalkerNetworkManager");
            }
        }
    }
}
