namespace OptimizedSkinwalkers
{
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using Unity.Netcode;

    public class SkinwalkerNetworkManagerHandler
    {
        public static void ClientConnectInitializer(Scene sceneName, LoadSceneMode _)
        {
            if (sceneName.name == "SampleSceneRelay")
            {
                GameObject networkManager = new GameObject("SkinwalkerNetworkManager");
                networkManager.AddComponent<NetworkObject>();
                networkManager.AddComponent<SkinwalkerNetworkManager>();
                Debug.Log("Initialized SkinwalkerNetworkManager");
            }
        }
    }
}
