namespace OptimizedSkinwalkers
{
    using BepInEx.Configuration;
    using System;
    using System.Collections.Generic;

    public static class SkinwalkerConfig
    {
        public static Dictionary<VoiceEnabled, ConfigEntry<bool>> ConfigEntries = new();
        public static ConfigEntry<float> VoiceLineFrequency;

        public static void InitConfig(ConfigFile configFile)
        {
            //TODO :: Shouldn't hard code these values
            VoiceLineFrequency = configFile.Bind("Voice Settings", "VoiceLineFrequency", 1f, "1 is the default, and voice lines will occur every " + SkinwalkerBehaviour.PLAY_INTERVAL_MIN + " to " + SkinwalkerBehaviour.PLAY_INTERVAL_MAX + " seconds per enemy. Setting this to 2 means they will occur twice as often, 0.5 means half as often, etc.");
            SkinwalkerLogger.Log($"VoiceLineFrequency; VALUE LOADED FROM CONFIG: {VoiceLineFrequency.Value}");

            foreach (VoiceEnabled voiceName in Enum.GetValues(typeof(VoiceEnabled)))
            {
                bool defaultValue;
                string description = "";

                switch (voiceName)
                {
                    case VoiceEnabled.OtherOutsideEnemies:
                        description = "Every outside enemies that aren't in the list above";
                        defaultValue = false;
                        break;
                    case VoiceEnabled.OtherInsideEnemies:
                        defaultValue = true;
                        description = "Every inside enemies that aren't in the list above";
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