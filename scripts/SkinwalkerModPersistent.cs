namespace OptimizedSkinwalkers
{
    using Dissonance;
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
        private const float FOLDER_SCAN_INTERVAL = 8f;

        private List<AudioClip> cachedAudio = new List<AudioClip>();
        private RoundManager roundManager;

        private bool cleaningAudioCache;
        private string audioFolderPath;
        private int skinwalkerCheckedCount;
        private float nextTimeToCheckFolder;

        public static SkinwalkerModPersistent Instance { get; private set; }

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

            SkinwalkerLogger.Log("Skinwalker Mod Object Initialized");

            transform.position = Vector3.zero; //TODO :: Probably useless, but who knows

            audioFolderPath = Path.Combine(Application.dataPath, "..", "Dissonance_Diagnostics");
            InitializeAudioFolder();
            EnableRecording();
        }

        private void Update()
        {
            if (!Directory.Exists(audioFolderPath))
            {
                return;
            }

            if (Time.realtimeSinceStartup > nextTimeToCheckFolder)
            {
                nextTimeToCheckFolder = Time.realtimeSinceStartup + FOLDER_SCAN_INTERVAL;
                ScanWavFiles();
            }

            HandleRoundManagerState();
            HandleSkinwalkerBehaviour();
        }

        private void OnApplicationQuit()
        {
            DisableRecording();

            if (Directory.Exists(audioFolderPath))
            {
                Directory.Delete(audioFolderPath, recursive: true);
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

        private void InitializeAudioFolder()
        {
            if (Directory.Exists(audioFolderPath))
            {
                Directory.Delete(audioFolderPath, recursive: true);
            }

            Directory.CreateDirectory(audioFolderPath);
        }

        private void EnableRecording()
        {
            DebugSettings.Instance.EnablePlaybackDiagnostics = true;
            DebugSettings.Instance.RecordFinalAudio = true;
        }

        private void ScanWavFiles()
        {
            //TODO :: Should probably go through every path instead of starting multiple Coroutines
            string[] audioFilePaths = Directory.GetFiles(audioFolderPath);
            foreach (string filePath in audioFilePaths)
            {
                StartCoroutine(CacheWavFile(filePath));
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

        private IEnumerator CacheWavFile(string path)
        {
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
            yield return request.SendWebRequest();

            while (request.result == Result.InProgress)
            {
                yield return null;
            }

            if (request.result != Result.Success)
            {
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            if (audioClip.length > 0.9f)
            {
                cachedAudio.Add(audioClip);
            }

            File.Delete(path);
            request.Dispose();
        }

        private void DisableRecording()
        {
            DebugSettings.Instance.EnablePlaybackDiagnostics = false;
            DebugSettings.Instance.RecordFinalAudio = false;
        }

        public bool TryGetSample(out AudioClip audioClip)
        {
            audioClip = null;

            if (cachedAudio.Count > 0)
            {
                int index = Random.Range(0, cachedAudio.Count - 1);
                audioClip = cachedAudio[index];
                cachedAudio.RemoveAt(index);

                if (!cleaningAudioCache && cachedAudio.Count > MAX_CACHED_AUDIO)
                {
                    StartCoroutine(CleanAudioCache());
                }

                if (audioClip == null)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private IEnumerator CleanAudioCache()
        {
            cleaningAudioCache = true;

            while (cachedAudio.Count > MAX_CACHED_AUDIO)
            {
                cachedAudio.RemoveAt(UnityEngine.Random.Range(0, cachedAudio.Count));
                yield return null;
            }

            cleaningAudioCache = false;
        }

        public void ClearCache()
        {
            cachedAudio.Clear();
        }
    }
}
