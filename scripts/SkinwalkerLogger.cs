namespace OptimizedSkinwalkers
{
    using BepInEx.Logging;

    public static class SkinwalkerLogger
    {
        internal static ManualLogSource logSource;

        public static void Initialize(string modGUID)
        {
            logSource = Logger.CreateLogSource(modGUID);
        }

        public static void Log(object message)
        {
            logSource.LogInfo(message);
        }

        public static void LogError(object message)
        {
            logSource.LogError(message);
        }

        public static void LogWarning(object message)
        {
            logSource.LogWarning(message);
        }
    }
}