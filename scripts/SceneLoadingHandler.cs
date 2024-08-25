namespace OptimizedSkinwalkers
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class SceneLoadingHandler : MonoBehaviour
    {
        private int internalSceneCount;
        private bool destroyHandler;

        private void Awake()
        {
            SkinwalkerLogger.Log("SceneLoadingHandler Awake");
            internalSceneCount = SceneManager.sceneCount;
        }

        private void Update()
        {
            if (SceneManager.sceneCount != internalSceneCount)
            {
                internalSceneCount = SceneManager.sceneCount;

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (SkinwalkerNetworkManagerHandler.TryClientConnectInitializer(scene))
                    {
                        destroyHandler = true;
                        break;
                    }
                }

                if (destroyHandler)
                {
                    SkinwalkerLogger.Log("Destroying SceneLoadingHandler...");
                    Destroy(gameObject);
                }
            }
        }
    }
}
