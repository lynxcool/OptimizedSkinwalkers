namespace OptimizedSkinwalkers
{
    using BepInEx.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    public static class SkinwalkerConfig
    {
        public const float DEFAULT_FOLDER_SCAN_INTERVAL = 8f;
        public const int CUSTOM_SOUND_FREQUENCY_MIN = 0;
        public const int CUSTOM_SOUND_FREQUENCY_MAX = 100;
        public const float DEFAULT_VOICE_FREQUENCY = 1f;
        public const bool DEFAULT_INSIDE_ENEMIES = true;
        public const bool DEFAULT_OUTSIDE_ENEMIES = true;
        public const bool DEFAULT_DAY_TIME_ENEMIES = false;
        public const bool DEFAULT_NIGHT_TIME_ENEMIES = true;

        private const string EXTRA_SETTINGS = "Extra Settings";
        private const string MONSTER_VOICES = "Monster Voices";

        public static ConfigEntry<float> VoiceLineFrequency;
        public static ConfigEntry<bool> KeepFilesBetweenSessions;
        public static ConfigEntry<float> TimeForFileCaching;
        public static ConfigEntry<bool> AddCustomFiles;
        public static ConfigEntry<float> CustomSoundFrequency;

        public static Dictionary<Type, EnemyConfigEntry> EnemyEntries = new();
        public static ConfigEntry<bool> InsideModdedEnemies;
        public static ConfigEntry<bool> OutsideModdedEnemies;
        public static ConfigEntry<bool> DayTimeModdedEnemies;
        public static ConfigEntry<bool> NightTimeModdedEnemies;

        public static void InitConfig(ConfigFile configFile)
        {
            BuildEnemyEntries(GetAllEnemyTypes());

            GenerateVoiceLineConfig(configFile);
            GenerateMonsterVoicesConfig(configFile);
            GenerateModdedMonsterConfig(configFile);
            GenerateExtraConfig(configFile);
        }

        private static void GenerateVoiceLineConfig(ConfigFile configFile)
        {
            VoiceLineFrequency = configFile.Bind("Voice Settings",
                                                "Voice Line Frequency",
                                                DEFAULT_VOICE_FREQUENCY,
                                                $"{DEFAULT_VOICE_FREQUENCY} is the default, and voice lines will occur every {SkinwalkerBehaviour.PLAY_INTERVAL_MIN} to {SkinwalkerBehaviour.PLAY_INTERVAL_MAX} seconds per enemy." +
                                                $"\nSetting this to 2 means they will occur twice as often, 0.5 means half as often, etc." +
                                                $"\nSetting this to 0 disables the mod.");
            SkinwalkerLogger.Log($"VoiceLineFrequency; VALUE LOADED FROM CONFIG: {VoiceLineFrequency.Value}");
        }

        private static void GenerateMonsterVoicesConfig(ConfigFile configFile)
        {
            foreach (EnemyConfigEntry enemyEntry in EnemyEntries.Values)
            {
                enemyEntry.SetConfigEntry(configFile, MONSTER_VOICES);
            }
        }

        private static void GenerateModdedMonsterConfig(ConfigFile configFile)
        {
            InsideModdedEnemies = configFile.Bind(MONSTER_VOICES, "Modded Enemies (Inside)", DEFAULT_INSIDE_ENEMIES);
            OutsideModdedEnemies = configFile.Bind(MONSTER_VOICES, "Modded Enemies (Outside)", DEFAULT_OUTSIDE_ENEMIES);
            DayTimeModdedEnemies = configFile.Bind(MONSTER_VOICES, "Modded Enemies (Day Time)", DEFAULT_DAY_TIME_ENEMIES);
            NightTimeModdedEnemies = configFile.Bind(MONSTER_VOICES, "Modded Enemies (Night Time)", DEFAULT_NIGHT_TIME_ENEMIES);
        }

        private static void GenerateExtraConfig(ConfigFile configFile)
        {
            KeepFilesBetweenSessions = configFile.Bind(EXTRA_SETTINGS,
                                                        "Keep Files Between Sessions",
                                                        false,
                                                        "If set to true, the content of Dissonance_Diagnostics won't be deleted on boot, keeping remaining sound files from previous sessions.");
            SkinwalkerLogger.Log($"KeepFilesBetweenSessions; VALUE LOADED FROM CONFIG: {KeepFilesBetweenSessions.Value}");

            TimeForFileCaching = configFile.Bind(EXTRA_SETTINGS,
                                                    "Time For File Caching",
                                                    DEFAULT_FOLDER_SCAN_INTERVAL,
                                                    "Dictates the interval (in seconds) at which we collect player recordings. Once recordings are collected, they're immediately deleted." +
                                                    "\nA higher number would increase the likelyhood of clips remaining for future sessions." +
                                                    $"\nValues under the default {DEFAULT_FOLDER_SCAN_INTERVAL} seconds will be ignored.");
            SkinwalkerLogger.Log($"TimeForFileCaching; VALUE LOADED FROM CONFIG: {TimeForFileCaching.Value}");

            AddCustomFiles = configFile.Bind(EXTRA_SETTINGS,
                                            "Add custom sounds",
                                            false,
                                            "If set to true, will create a folder Custom_Sounds in which you can put your own audio files." +
                                            "\nNote that other players won't be able to hear these sounds, only you, therefore the folder content should be shared between players." +
                                            "\nUnlike the recorded voice lines, these files won't be deleted.");
            SkinwalkerLogger.Log($"AddCustomFiles; VALUE LOADED FROM CONFIG: {AddCustomFiles.Value}");

            CustomSoundFrequency = configFile.Bind(EXTRA_SETTINGS,
                                                    "Custom sounds frequency",
                                                    50f,
                                                    $"Value between {CUSTOM_SOUND_FREQUENCY_MIN} and {CUSTOM_SOUND_FREQUENCY_MAX}. Dictates the frequency at which to play custom sounds vs recorded lines." +
                                                    $"\nIf set to {CUSTOM_SOUND_FREQUENCY_MIN}, only the recorded lines will play, deactivating the previous option." +
                                                    $"\nIf set to {CUSTOM_SOUND_FREQUENCY_MAX}, only the custom sounds will play, disabling the voice line recording.");
            SkinwalkerLogger.Log($"CustomSoundFrequency; VALUE LOADED FROM CONFIG: {CustomSoundFrequency.Value}");
        }

        private static void BuildEnemyEntries(List<Type> enemyTypes)
        {
            foreach (Type enemyType in enemyTypes)
            {
                EnemyEntries.Add(enemyType, new EnemyConfigEntry(enemyType));
            }
        }

        private static List<Type> GetAllEnemyTypes()
        {
            List<Type> enemyDerivatives = new();
            Assembly assembly;

            try
            {
                assembly = Assembly.Load("Assembly-CSharp");
            }
            catch (Exception ex)
            {
                SkinwalkerLogger.LogError("Couldn't load Assembly-CSharp. Exception:");
                SkinwalkerLogger.LogError(ex.Message);
                return enemyDerivatives;
            }

            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                if (typeof(EnemyAI).IsAssignableFrom(type))
                {
                    if (type == typeof(TestEnemy) || type == typeof(EnemyAI))
                    {
                        continue;
                    }

                    enemyDerivatives.Add(type);
                }
            }

            return enemyDerivatives;
        }
    }
}