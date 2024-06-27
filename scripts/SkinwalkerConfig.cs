namespace OptimizedSkinwalkers
{
    using BepInEx.Configuration;
    using System;
    using System.Collections.Generic;

    public static class SkinwalkerConfig
    {
        public const float DEFAULT_FOLDER_SCAN_INTERVAL = 8f;
        public const int CUSTOM_SOUND_FREQUENCY_MIN = 0;
        public const int CUSTOM_SOUND_FREQUENCY_MAX = 100;

        private const float DEFAULT_FREQUENCY = 1f;
        private const string EXTRA_SETTINGS = "Extra Settings";

        public static ConfigEntry<float> VoiceLineFrequency;
        public static ConfigEntry<bool> KeepFilesBetweenSessions;
        public static ConfigEntry<float> TimeForFileCaching;
        public static ConfigEntry<bool> AddCustomFiles;
        public static ConfigEntry<float> CustomSoundFrequency;
        public static Dictionary<VoiceEnabled, ConfigEntry<bool>> ConfigEntries = new();

        public static void InitConfig(ConfigFile configFile)
        {
            //TODO :: Shouldn't hard code these values
            VoiceLineFrequency = configFile.Bind("Voice Settings",
                                                "VoiceLineFrequency",
                                                DEFAULT_FREQUENCY,
                                                $"{DEFAULT_FREQUENCY} is the default, and voice lines will occur every {SkinwalkerBehaviour.PLAY_INTERVAL_MIN} to {SkinwalkerBehaviour.PLAY_INTERVAL_MAX} seconds per enemy." +
                                                $"\nSetting this to 2 means they will occur twice as often, 0.5 means half as often, etc." +
                                                $"\nSetting this to 0 disables the mod.");
            SkinwalkerLogger.Log($"VoiceLineFrequency; VALUE LOADED FROM CONFIG: {VoiceLineFrequency.Value}");

            GenerateMonsterVoicesConfig(configFile);

            //TODO :: Add logs for these
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

        private static void GenerateMonsterVoicesConfig(ConfigFile configFile)
        {
            foreach (VoiceEnabled voiceName in Enum.GetValues(typeof(VoiceEnabled)))
            {
                bool defaultValue;
                string description = "";

                switch (voiceName)
                {
                    case VoiceEnabled.OtherOutsideEnemies:
                        defaultValue = false;
                        description = "Every OUTSIDE enemies that aren't in the list above.";
                        break;
                    case VoiceEnabled.OtherInsideEnemies:
                        defaultValue = true;
                        description = "Every INSIDE enemies that aren't in the list above.";
                        break;
                    default:
                        defaultValue = true;
                        break;
                }

                ConfigEntry<bool> entry = configFile.Bind("Monster Voices", voiceName.ToString(), defaultValue, description);
                ConfigEntries.Add(voiceName, entry);

                SkinwalkerLogger.Log($"VoiceEnabled_{voiceName}; VALUE LOADED FROM CONFIG: {ConfigEntries[voiceName].Value}");
            }
        }
    }
}