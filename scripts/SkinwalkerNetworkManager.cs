namespace OptimizedSkinwalkers
{
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;

    public class SkinwalkerNetworkManager : NetworkBehaviour
    {
        public Dictionary<Type, NetworkVariable<bool>> NetworkVariablesDict = new();
        public NetworkVariable<bool> InsideModdedEnemy;
        public NetworkVariable<bool> OutsideModdedEnemy;
        public NetworkVariable<bool> DayTimeModdedEnemy;
        public NetworkVariable<bool> NightTimeModdedEnemy;
        public NetworkVariable<float> VoiceLineFrequency;

        private readonly NetworkVariableReadPermission readPermission = NetworkVariableReadPermission.Everyone;
        private readonly NetworkVariableWritePermission writePermission = NetworkVariableWritePermission.Server;

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

            InsideModdedEnemy = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_INSIDE_ENEMIES, readPermission, writePermission);
            OutsideModdedEnemy = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_OUTSIDE_ENEMIES, readPermission, writePermission);
            DayTimeModdedEnemy = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_DAY_TIME_ENEMIES, readPermission, writePermission);
            NightTimeModdedEnemy = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_NIGHT_TIME_ENEMIES, readPermission, writePermission);
            VoiceLineFrequency = new NetworkVariable<float>(SkinwalkerConfig.DEFAULT_VOICE_FREQUENCY, readPermission, writePermission);
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
            SkinwalkerLogger.Log("called __initializeVariables");

            if (!TryInitializeNetworkVariables() || !TryInitializeEnemyNetworkVariables())
            {
                SkinwalkerLogger.LogError("NetworkManager Initialization failed");
            }
        }

        protected override string __getTypeName()
        {
            return "SkinwalkerNetworkManager";
        }

        private void BuildNetworkVariableDict()
        {
            foreach (EnemyConfigEntry configEntry in SkinwalkerConfig.ConfigEntries.Values)
            {
                NetworkVariable<bool> networkVariable = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
                NetworkVariablesDict.Add(configEntry.EnemyType, networkVariable);
            }
        }

        private bool TryInitializeNetworkVariables()
        {
            if (VoiceLineFrequency == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.VoiceLineFrequency cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (InsideModdedEnemy == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.InsideModdedEnemy cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (OutsideModdedEnemy == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.OutsideModdedEnemy cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (DayTimeModdedEnemy == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.DayTimeModdedEnemy cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (NightTimeModdedEnemy == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NightTimeModdedEnemy cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (VoiceLineFrequency == null || InsideModdedEnemy == null || OutsideModdedEnemy == null || DayTimeModdedEnemy == null || NightTimeModdedEnemy == null)
            {
                return false;
            }

            VoiceLineFrequency.Initialize(this);
            __nameNetworkVariable(VoiceLineFrequency, $"VoiceLineFrequency");
            NetworkVariableFields.Add(VoiceLineFrequency);

            InsideModdedEnemy.Initialize(this);
            __nameNetworkVariable(InsideModdedEnemy, $"VoiceEnabled_InsideModdedEnemy");
            NetworkVariableFields.Add(InsideModdedEnemy);

            OutsideModdedEnemy.Initialize(this);
            __nameNetworkVariable(OutsideModdedEnemy, $"VoiceEnabled_OutsideModdedEnemy");
            NetworkVariableFields.Add(OutsideModdedEnemy);

            DayTimeModdedEnemy.Initialize(this);
            __nameNetworkVariable(DayTimeModdedEnemy, $"VoiceEnabled_DayTimeModdedEnemy");
            NetworkVariableFields.Add(DayTimeModdedEnemy);

            NightTimeModdedEnemy.Initialize(this);
            __nameNetworkVariable(NightTimeModdedEnemy, $"VoiceEnabled_NightTimeModdedEnemy");
            NetworkVariableFields.Add(NightTimeModdedEnemy);

            return true;
        }

        private bool TryInitializeEnemyNetworkVariables()
        {
            foreach (EnemyConfigEntry configEntry in SkinwalkerConfig.ConfigEntries.Values)
            {
                if (NetworkVariablesDict[configEntry.EnemyType] == null)
                {
                    SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NetworkVariables.Key:{configEntry.EnemyType.Name} cannot be null. All NetworkVariableBase instances must be initialized.");
                    return false;
                }

                NetworkVariablesDict[configEntry.EnemyType].Initialize(this);
                __nameNetworkVariable(NetworkVariablesDict[configEntry.EnemyType], $"VoiceEnabled_{configEntry.cleanedName}");
                NetworkVariableFields.Add(NetworkVariablesDict[configEntry.EnemyType]);
            }

            return true;
        }

        private void SetupConfig()
        {
            SkinwalkerLogger.Log("SetupConfig");

            if (GameNetworkManager.Instance.isHostingGame)
            {
                SkinwalkerLogger.Log("HOST SENDING CONFIG TO CLIENTS...");

                VoiceLineFrequency.Value = SkinwalkerConfig.VoiceLineFrequency.Value;
                InsideModdedEnemy.Value = SkinwalkerConfig.InsideModdedEnemies.Value;
                OutsideModdedEnemy.Value = SkinwalkerConfig.OutsideModdedEnemies.Value;
                DayTimeModdedEnemy.Value = SkinwalkerConfig.DayTimeModdedEnemies.Value;
                NightTimeModdedEnemy.Value = SkinwalkerConfig.NightTimeModdedEnemies.Value;

                foreach (EnemyConfigEntry configEntry in SkinwalkerConfig.ConfigEntries.Values)
                {
                    if (NetworkVariablesDict[configEntry.EnemyType] == null)
                    {
                        SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NetworkVariables.Key:{configEntry.EnemyType.Name} was null");
                        return;
                    }

                    NetworkVariablesDict[configEntry.EnemyType].Value = SkinwalkerConfig.ConfigEntries[configEntry.EnemyType].configEntry.Value;
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