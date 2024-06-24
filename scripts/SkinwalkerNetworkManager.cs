namespace OptimizedSkinwalkers
{
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;

    public class SkinwalkerNetworkManager : NetworkBehaviour
    {
        public Dictionary<VoiceEnabled, NetworkVariable<bool>> NetworkVariables = new();
        public NetworkVariable<float> VoiceLineFrequency;

        public static SkinwalkerNetworkManager Instance { get; private set; }

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

            SkinwalkerLogger.Log("SkinwalkerNetworkManager Awake Called");

            VoiceLineFrequency = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            BuildNetworkVariableDict();
        }

        public override void OnDestroy()
        {
            SkinwalkerLogger.Log("SkinwalkerNetworkManager OnDestroy");

            if (SkinwalkerModPersistent.Instance != null)
            {
                SkinwalkerModPersistent.Instance.ClearCache();
            }

            Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            SkinwalkerLogger.Log("OnNetworkSpawn");
            SetupConfig();
        }

        protected override void __initializeVariables()
        {
            SkinwalkerLogger.Log("__initializeVariables");

            if (VoiceLineFrequency == null)
            {
                SkinwalkerLogger.LogError("SkinwalkerNetworkManager.VoiceLineFrequency cannot be null. All NetworkVariableBase instances must be initialized.");
                return;
            }

            VoiceLineFrequency.Initialize(this);
            __nameNetworkVariable(VoiceLineFrequency, $"VoiceEnabled_VoiceLineFrequency");
            NetworkVariableFields.Add(VoiceLineFrequency);

            for (int i = 0; i < NetworkVariables.Count; i++)
            {
                VoiceEnabled voiceValue = (VoiceEnabled)i;

                if (NetworkVariables[voiceValue] == null)
                {
                    SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NetworkVariables.Key:{voiceValue} cannot be null. All NetworkVariableBase instances must be initialized.");
                    return;
                }

                NetworkVariables[voiceValue].Initialize(this);
                __nameNetworkVariable(NetworkVariables[voiceValue], $"VoiceEnabled_{voiceValue.ToString()}");
                NetworkVariableFields.Add(NetworkVariables[voiceValue]);
            }
        }

        protected override string __getTypeName()
        {
            return "SkinwalkerNetworkManager";
        }

        private void BuildNetworkVariableDict()
        {
            for (int i = 0; i < Enum.GetValues(typeof(VoiceEnabled)).Length; i++)
            {
                VoiceEnabled voiceValue = (VoiceEnabled)i;

                NetworkVariable<bool> networkVariable = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
                NetworkVariables.Add(voiceValue, networkVariable);
            }
        }

        private void SetupConfig()
        {
            SkinwalkerLogger.Log("SetupConfig");

            if (GameNetworkManager.Instance.isHostingGame)
            {
                SkinwalkerLogger.Log("HOST SENDING CONFIG TO CLIENTS...");

                VoiceLineFrequency.Value = SkinwalkerConfig.VoiceLineFrequency.Value;

                for (int i = 0; i < NetworkVariables.Count; i++)
                {
                    VoiceEnabled voiceValue = (VoiceEnabled)i;

                    if (NetworkVariables[voiceValue] == null)
                    {
                        SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NetworkVariables.Key:{voiceValue} was null");
                        return;
                    }

                    NetworkVariables[voiceValue].Value = SkinwalkerConfig.ConfigEntries[voiceValue].Value;
                }

                SkinwalkerLogger.Log("CONFIG SENT...");
            }
            else
            {
                SkinwalkerLogger.Log("Not the host, no config to send");
            }
        }
    }
}