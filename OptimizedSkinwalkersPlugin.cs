namespace OptimizedSkinwalkers
{
    using BepInEx;
    using Dissonance;
    using HarmonyLib;
    using System;
    using System.Reflection;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    //PluginInfo's values are set based on the .csproj values:
    //AssemblyName AND Version
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class OptimizedSkinwalkersPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public static OptimizedSkinwalkersPlugin Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            InvokeAssemblyMethods();

            SkinwalkerLogger.Initialize(PluginInfo.PLUGIN_GUID);
            SkinwalkerLogger.Log($"SKINWALKER MOD STARTING UP {PluginInfo.PLUGIN_VERSION}");
            SkinwalkerConfig.InitConfig(Config);

            if (SkinwalkerConfig.VoiceLineFrequency.Value == 0f)
            {
                SkinwalkerLogger.LogWarning("VoiceLineFrequency set to 0. Aborting mod initialization");
                return;
            }

            InitializeNetworkVariableSerializationTypes();
            SceneManager.sceneLoaded += SkinwalkerNetworkManagerHandler.ClientConnectInitializer;

            GameObject modPersistent = new("Skinwalker Mod");
            modPersistent.AddComponent<SkinwalkerModPersistent>();
            modPersistent.hideFlags = (HideFlags)61;
            DontDestroyOnLoad(modPersistent);

            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                if (category == LogCategory.Core)
                {
                    continue;
                }

                Logs.SetLogLevel(category, Dissonance.LogLevel.Error);
            }
        }

        private void InvokeAssemblyMethods()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                foreach (MethodInfo methodInfo in methods)
                {
                    object[] customAttributes = methodInfo.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), inherit: false);
                    if (customAttributes.Length != 0)
                    {
                        methodInfo.Invoke(null, null);
                    }
                }
            }
        }

        private void InitializeNetworkVariableSerializationTypes()
        {
            NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy<bool>();
            NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable<bool>();
            NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy<float>();
            NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable<float>();
        }
    }
}