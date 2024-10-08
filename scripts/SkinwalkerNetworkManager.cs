﻿namespace OptimizedSkinwalkers
{
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;

    public class SkinwalkerNetworkManager : NetworkBehaviour
    {
        public Dictionary<Type, NetworkVariable<bool>> NetworkVariablesDict = new();
        public NetworkVariable<bool> InsideModdedEnemies;
        public NetworkVariable<bool> OutsideModdedEnemies;
        public NetworkVariable<bool> DayTimeModdedEnemies;
        public NetworkVariable<bool> NightTimeModdedEnemies;
        public NetworkVariable<bool> UnspecifiedModdedEnemies;
        public NetworkVariable<float> VoiceLineFrequency;

        private readonly NetworkVariableReadPermission readPermission = NetworkVariableReadPermission.Everyone;
        private readonly NetworkVariableWritePermission writePermission = NetworkVariableWritePermission.Server;

        public static SkinwalkerNetworkManager Instance { get; private set; }

        private void Awake()
        {
            SkinwalkerLogger.Log("SkinwalkerNetworkManager Awake Called");

            if (Instance == null)
            {
                SkinwalkerLogger.Log("SkinwalkerNetworkManager Instance set");
                Instance = this;
            }
            else if (Instance != this) 
            {
                SkinwalkerLogger.Log("SkinwalkerNetworkManager Destroying duplicate NetworkManager...");
                Destroy(this);
                return;
            }

            InsideModdedEnemies = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_INSIDE_ENEMIES, readPermission, writePermission);
            OutsideModdedEnemies = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_OUTSIDE_ENEMIES, readPermission, writePermission);
            DayTimeModdedEnemies = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_DAYTIME_ENEMIES, readPermission, writePermission);
            NightTimeModdedEnemies = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_NIGHTTIME_ENEMIES, readPermission, writePermission);
            UnspecifiedModdedEnemies = new NetworkVariable<bool>(SkinwalkerConfig.DEFAULT_UNSPECIFIED_ENEMIES, readPermission, writePermission);
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
            foreach (EnemyConfigEntry configEntry in SkinwalkerConfig.EnemyEntries.Values)
            {
                NetworkVariable<bool> networkVariable = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
                NetworkVariablesDict.Add(configEntry.EnemyType, networkVariable);
            }
        }

        private bool TryInitializeNetworkVariables()
        {
            if (VoiceLineFrequency == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(VoiceLineFrequency)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (InsideModdedEnemies == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(InsideModdedEnemies)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (OutsideModdedEnemies == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(OutsideModdedEnemies)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (DayTimeModdedEnemies == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(DayTimeModdedEnemies)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (NightTimeModdedEnemies == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(NightTimeModdedEnemies)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (UnspecifiedModdedEnemies == null)
            {
                SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.{nameof(UnspecifiedModdedEnemies)} cannot be null. All NetworkVariableBase instances must be initialized.");
            }

            if (VoiceLineFrequency == null ||
                InsideModdedEnemies == null ||
                OutsideModdedEnemies == null ||
                DayTimeModdedEnemies == null ||
                NightTimeModdedEnemies == null ||
                UnspecifiedModdedEnemies == null)
            {
                return false;
            }

            VoiceLineFrequency.Initialize(this);
            __nameNetworkVariable(VoiceLineFrequency, nameof(VoiceLineFrequency));
            NetworkVariableFields.Add(VoiceLineFrequency);

            InsideModdedEnemies.Initialize(this);
            __nameNetworkVariable(InsideModdedEnemies, $"VoiceEnabled_{nameof(InsideModdedEnemies)}");
            NetworkVariableFields.Add(InsideModdedEnemies);

            OutsideModdedEnemies.Initialize(this);
            __nameNetworkVariable(OutsideModdedEnemies, $"VoiceEnabled_{nameof(OutsideModdedEnemies)}");
            NetworkVariableFields.Add(OutsideModdedEnemies);

            DayTimeModdedEnemies.Initialize(this);
            __nameNetworkVariable(DayTimeModdedEnemies, $"VoiceEnabled_{nameof(DayTimeModdedEnemies)}");
            NetworkVariableFields.Add(DayTimeModdedEnemies);

            NightTimeModdedEnemies.Initialize(this);
            __nameNetworkVariable(NightTimeModdedEnemies, $"VoiceEnabled_{nameof(NightTimeModdedEnemies)}");
            NetworkVariableFields.Add(NightTimeModdedEnemies);

            UnspecifiedModdedEnemies.Initialize(this);
            __nameNetworkVariable(UnspecifiedModdedEnemies, $"VoiceEnabled_{nameof(UnspecifiedModdedEnemies)}");
            NetworkVariableFields.Add(UnspecifiedModdedEnemies);

            return true;
        }

        private bool TryInitializeEnemyNetworkVariables()
        {
            foreach (EnemyConfigEntry configEntry in SkinwalkerConfig.EnemyEntries.Values)
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
                InsideModdedEnemies.Value = SkinwalkerConfig.InsideModdedEnemies.Value;
                OutsideModdedEnemies.Value = SkinwalkerConfig.OutsideModdedEnemies.Value;
                DayTimeModdedEnemies.Value = SkinwalkerConfig.DayTimeModdedEnemies.Value;
                NightTimeModdedEnemies.Value = SkinwalkerConfig.NightTimeModdedEnemies.Value;
                UnspecifiedModdedEnemies.Value = SkinwalkerConfig.UnspecifiedModdedEnemies.Value;

                SkinwalkerLogger.Log($"Entering foreach");
                foreach (EnemyConfigEntry enemyEntry in SkinwalkerConfig.EnemyEntries.Values)
                {
                    if (NetworkVariablesDict[enemyEntry.EnemyType] == null)
                    {
                        SkinwalkerLogger.LogError($"SkinwalkerNetworkManager.NetworkVariables.Key:{enemyEntry.EnemyType.Name} was null");
                        return;
                    }

                    NetworkVariablesDict[enemyEntry.EnemyType].Value = enemyEntry.configEntry.Value;
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