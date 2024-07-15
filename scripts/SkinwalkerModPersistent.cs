namespace OptimizedSkinwalkers
{
    using Dissonance.Config;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.Networking;
    using static UnityEngine.Networking.UnityWebRequest;

    public class SkinwalkerModPersistent : MonoBehaviour
    {
        private const int MAX_CACHED_AUDIO = 200;

        private List<AudioClip> cachedRecordedAudio = new();
        private List<AudioClip> cachedCustomAudio = new();
        private RoundManager roundManager;

        private bool isCleaningAudioCache;

        private string recordedAudioFolderPath;
        private string customAudioFolderPath;
        private bool recordedFolderCreated;
        private bool customFolderCreated;

        private int skinwalkerCheckedCount;

        private float folderScanRate;
        private float nextTimeToCheckFolder;

        public static SkinwalkerModPersistent Instance { get; private set; }

        private bool IsVoiceRecordingEnabled => !(SkinwalkerConfig.AddCustomFiles.Value && SkinwalkerConfig.CustomSoundFrequency.Value >= SkinwalkerConfig.CUSTOM_SOUND_FREQUENCY_MAX);
        private bool AreCustomSoundsEnabled => SkinwalkerConfig.AddCustomFiles.Value && SkinwalkerConfig.CustomSoundFrequency.Value > SkinwalkerConfig.CUSTOM_SOUND_FREQUENCY_MIN;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }

            SkinwalkerLogger.Log("SkinwalkerModPersistent Initialized");
            SkinwalkerLogger.Log($"IsVoiceRecordingEnabled {IsVoiceRecordingEnabled}");
            SkinwalkerLogger.Log($"AreCustomSoundsEnabled {AreCustomSoundsEnabled}");

            transform.position = Vector3.zero; //TODO :: Probably useless, but who knows

            recordedAudioFolderPath = Path.Combine(Application.dataPath, "..", "Dissonance_Diagnostics");
            customAudioFolderPath = Path.Combine(Application.dataPath, "..", "Custom_Sounds");
            InitializeAudioFolders();

            folderScanRate = Mathf.Max(SkinwalkerConfig.DEFAULT_FOLDER_SCAN_INTERVAL, SkinwalkerConfig.TimeForFileCaching.Value);

            if (IsVoiceRecordingEnabled)
            {
                EnableRecording();
            }
        }

        private void Update()
        {
            if ((IsVoiceRecordingEnabled && !recordedFolderCreated) ||
                AreCustomSoundsEnabled && !customFolderCreated)
            {
                return;
            }

            if (IsVoiceRecordingEnabled && Time.realtimeSinceStartup > nextTimeToCheckFolder)
            {
                nextTimeToCheckFolder = Time.realtimeSinceStartup + folderScanRate;
                ScanThenCacheWavFiles(recordedAudioFolderPath, cachedRecordedAudio, true);
            }

            HandleRoundManagerState();
            HandleSkinwalkerBehaviour();
        }

        private void OnApplicationQuit()
        {
            DisableRecording();

            if (!SkinwalkerConfig.KeepFilesBetweenSessions.Value && Directory.Exists(recordedAudioFolderPath))
            {
                Directory.Delete(recordedAudioFolderPath, recursive: true);
            }
        }

        private void HandleRoundManagerState()
        {
            if (roundManager == null && RoundManager.Instance != null)
            {
                roundManager = RoundManager.Instance;
                skinwalkerCheckedCount = RoundManager.Instance.SpawnedEnemies.Count;
            }
        }

        private void HandleSkinwalkerBehaviour()
        {
            if (RoundManager.Instance == null)
            {
                return;
            }

            int enemiesCount = RoundManager.Instance.SpawnedEnemies.Count;
            if (skinwalkerCheckedCount == enemiesCount)
            {
                return;
            }

            if (skinwalkerCheckedCount < enemiesCount)
            {
                foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
                {
                    if (IsEnemyEnabled(enemy) && !enemy.TryGetComponent<SkinwalkerBehaviour>(out _))
                    {
                        enemy.gameObject.AddComponent<SkinwalkerBehaviour>().Initialize(enemy);
                    }
                }
            }

            skinwalkerCheckedCount = enemiesCount;
        }

        private void InitializeAudioFolders()
        {
            if (IsVoiceRecordingEnabled)
            {
                if (!Directory.Exists(recordedAudioFolderPath))
                {
                    Directory.CreateDirectory(recordedAudioFolderPath);
                }
                else if (!SkinwalkerConfig.KeepFilesBetweenSessions.Value)
                {
                    Directory.Delete(recordedAudioFolderPath, true);
                    Directory.CreateDirectory(recordedAudioFolderPath);
                }
            }

            if (AreCustomSoundsEnabled)
            {
                if (!Directory.Exists(customAudioFolderPath))
                {
                    Directory.CreateDirectory(customAudioFolderPath);
                }
            }

            StartCoroutine(CheckForFolderCreation());
        }

        private IEnumerator CheckForFolderCreation()
        {
            while (IsVoiceRecordingEnabled && !recordedFolderCreated)
            {
                recordedFolderCreated = Directory.Exists(recordedAudioFolderPath);

                if (!recordedFolderCreated)
                {
                    yield return new WaitForSecondsRealtime(1f);
                }
            }

            while (AreCustomSoundsEnabled && !customFolderCreated)
            {
                customFolderCreated = Directory.Exists(customAudioFolderPath);

                if (customFolderCreated)
                {
                    ScanThenCacheWavFiles(customAudioFolderPath, cachedCustomAudio, false);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1f);
                }
            }
        }

        private void EnableRecording()
        {
            DebugSettings.Instance.EnablePlaybackDiagnostics = true;
            DebugSettings.Instance.RecordFinalAudio = true;
        }

        private void ScanThenCacheWavFiles(string path, List<AudioClip> cacheList, bool deleteAfterCaching)
        {
            string[] audioFilePaths = Directory.GetFiles(path);

            if (audioFilePaths.Length > 0)
            {
                StartCoroutine(CacheWavFile(audioFilePaths, cacheList, deleteAfterCaching));
            }
        }

        private bool IsEnemyEnabled(EnemyAI enemy)
        {
            if (enemy == null)
            {
                SkinwalkerLogger.LogError("enemy was null");
                return false;
            }

            if (SkinwalkerNetworkManager.Instance.NetworkVariablesDict.TryGetValue(enemy.GetType(), out NetworkVariable<bool> networkVariable))
            {
                return networkVariable.Value;
            }
            else
            {
                if (enemy.enemyType.isOutsideEnemy && !SkinwalkerNetworkManager.Instance.OutsideModdedEnemies.Value)
                {
                    return false;
                }
                
                if (!enemy.enemyType.isOutsideEnemy && !SkinwalkerNetworkManager.Instance.InsideModdedEnemies.Value)
                {
                    return false;
                }

                if (enemy.enemyType.isDaytimeEnemy && !SkinwalkerNetworkManager.Instance.DayTimeModdedEnemies.Value)
                {
                    return false;
                }

                if (!enemy.enemyType.isDaytimeEnemy && !SkinwalkerNetworkManager.Instance.NightTimeModdedEnemies.Value)
                {
                    return false;
                }

                return true;
            }
        }

        private IEnumerator CacheWavFile(string[] paths, List<AudioClip> referencedList, bool deleteAfterCaching)
        {
            foreach (string path in paths)
            {
                UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
                yield return request.SendWebRequest(); //TODO :: Do we need to yield for this?

                while (request.result == Result.InProgress)
                {
                    yield return null;
                }

                if (request.result != Result.Success)
                {
                    SkinwalkerLogger.LogWarning($"Request failed for file at path: {path}\n Request Result: {request.result}");
                    continue;
                }

                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                if (!deleteAfterCaching || audioClip.length > 0.9f)
                {
                    referencedList.Add(audioClip);
                }

                if (deleteAfterCaching)
                {
                    File.Delete(path);
                }
                request.Dispose();
            }

            if (!isCleaningAudioCache && cachedRecordedAudio.Count > MAX_CACHED_AUDIO)
            {
                StartCoroutine(CleanAudioCache());
            }
        }

        private void DisableRecording()
        {
            DebugSettings.Instance.EnablePlaybackDiagnostics = false;
            DebugSettings.Instance.RecordFinalAudio = false;
        }

        public bool TryGetSample(out AudioClip audioClip)
        {
            audioClip = null;
            List<AudioClip> chosenList;
            bool removeOncePlayed;

            if (IsVoiceRecordingEnabled && AreCustomSoundsEnabled)
            {
                //TODO :: Rolling a float between 0 and 1 seemed to give strange odds. Using ints for now.
                float random = UnityEngine.Random.Range(SkinwalkerConfig.CUSTOM_SOUND_FREQUENCY_MIN, SkinwalkerConfig.CUSTOM_SOUND_FREQUENCY_MAX + 1);
                removeOncePlayed = random > SkinwalkerConfig.CustomSoundFrequency.Value;
                chosenList = removeOncePlayed ? cachedRecordedAudio : cachedCustomAudio;
            }
            else if (IsVoiceRecordingEnabled)
            {
                chosenList = cachedRecordedAudio;
                removeOncePlayed = true;
            }
            else
            {
                chosenList = cachedCustomAudio;
                removeOncePlayed = false;
            }

            if (chosenList.Count == 0)
            {
                return false;
            }

            int index = UnityEngine.Random.Range(0, chosenList.Count);
            audioClip = chosenList[index];

            if (removeOncePlayed)
            {
                chosenList.RemoveAt(index);
            }

            if (audioClip == null)
            {
                SkinwalkerLogger.LogWarning($"TryGetSample.audioClip was null");
                return false;
            }

            return true;
        }

        private IEnumerator CleanAudioCache()
        {
            isCleaningAudioCache = true;

            while (cachedRecordedAudio.Count > MAX_CACHED_AUDIO)
            {
                cachedRecordedAudio.RemoveAt(UnityEngine.Random.Range(0, cachedRecordedAudio.Count));
                yield return null;
            }

            isCleaningAudioCache = false;
        }

        public void ClearCache()
        {
            cachedRecordedAudio.Clear();
        }
    }
}
