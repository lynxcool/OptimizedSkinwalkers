namespace OptimizedSkinwalkers
{
    using Dissonance.Config;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using static UnityEngine.Networking.UnityWebRequest;

    public class SkinwalkerModPersistent : MonoBehaviour
    {
        private const int MAX_CACHED_AUDIO = 200;

        private List<AudioClip> cachedRecordedAudio = new();
        private List<AudioClip> cachedPersistentAudio = new();
        private RoundManager roundManager;

        private bool isCleaningAudioCache;

        private string recordedAudioFolderPath;
        private string persistentAudioFolderPath;
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
            persistentAudioFolderPath = Path.Combine(Application.dataPath, "..", "Custom_Sounds");
            InitializeAudioFolders();

            folderScanRate = Mathf.Max(SkinwalkerConfig.DEFAULT_FOLDER_SCAN_INTERVAL, SkinwalkerConfig.TimeForFileCaching.Value);

            if (IsVoiceRecordingEnabled)
            {
                EnableRecording();
            }
        }

        private IEnumerator Start()
        {
            if (AreCustomSoundsEnabled)
            {
                string[] audioFilePaths = Directory.GetFiles(persistentAudioFolderPath);
                yield return StartCoroutine(CacheWavFile(audioFilePaths, cachedPersistentAudio, false));
            }
        }

        private void Update()
        {
            if ((IsVoiceRecordingEnabled && !Directory.Exists(recordedAudioFolderPath)) ||
                AreCustomSoundsEnabled && !Directory.Exists(persistentAudioFolderPath))
            {
                return;
            }

            if (Time.realtimeSinceStartup > nextTimeToCheckFolder)
            {
                nextTimeToCheckFolder = Time.realtimeSinceStartup + folderScanRate;
                ScanWavFiles();
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
                if (!Directory.Exists(persistentAudioFolderPath))
                {
                    Directory.CreateDirectory(persistentAudioFolderPath);
                }
            }
        }

        private void EnableRecording()
        {
            DebugSettings.Instance.EnablePlaybackDiagnostics = true;
            DebugSettings.Instance.RecordFinalAudio = true;
        }

        private void ScanWavFiles()
        {
            string[] audioFilePaths = Directory.GetFiles(recordedAudioFolderPath);

            if (audioFilePaths.Length > 0)
            {
                StartCoroutine(CacheWavFile(audioFilePaths, cachedRecordedAudio, true));
            }
        }

        private bool IsEnemyEnabled(EnemyAI enemy)
        {
            if (enemy == null)
            {
                SkinwalkerLogger.LogError("enemy was null");
                return false;
            }

            switch (enemy)
            {
                case MaskedPlayerEnemy:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Masked].Value;
                case NutcrackerEnemyAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Nutcracker].Value;
                case BaboonBirdAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.BaboonHawk].Value;
                case FlowermanAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Bracken].Value;
                case SandSpiderAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.BunkerSpider].Value;
                case CentipedeAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Centipede].Value;
                case SpringManAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.CoilHead].Value;
                case MouthDogAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.EyelessDog].Value;
                case ForestGiantAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.ForestGiant].Value;
                case DressGirlAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.GhostGirl].Value;
                case SandWormAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.GiantWorm].Value;
                case HoarderBugAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.HoardingBug].Value;
                case BlobAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Hygrodere].Value;
                case JesterAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Jester].Value;
                case PufferAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.SporeLizard].Value;
                case CrawlerAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Thumper].Value;
                case ButlerEnemyAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.Butler].Value;
                case ButlerBeesEnemyAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.ButlerBees].Value;
                case RadMechAI:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.OldBird].Value;
                case FlowerSnakeEnemy:
                    return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.FlowerSnake].Value;
                default:
                    if (enemy.enemyType.isOutsideEnemy)
                    {
                        return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.OtherOutsideEnemies].Value;
                    }
                    else
                    {
                        return SkinwalkerNetworkManager.Instance.NetworkVariables[VoiceEnabled.OtherInsideEnemies].Value;
                    }
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
                if (audioClip.length > 0.9f)
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
                chosenList = removeOncePlayed ? cachedRecordedAudio : cachedPersistentAudio;
            }
            else if (IsVoiceRecordingEnabled)
            {
                chosenList = cachedRecordedAudio;
                removeOncePlayed = true;
            }
            else
            {
                chosenList = cachedPersistentAudio;
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
