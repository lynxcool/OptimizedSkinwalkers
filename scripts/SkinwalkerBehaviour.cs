namespace OptimizedSkinwalkers
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public class SkinwalkerBehaviour : MonoBehaviour
    {
        private AudioSource audioSource;

        public const float PLAY_INTERVAL_MIN = 15f;
        public const float PLAY_INTERVAL_MAX = 40f;
        private const float MAX_DIST = 100f;

        private float nextTimeToPlayAudio;

        private EnemyAI ai;

        public void Initialize(EnemyAI ai)
        {
            this.ai = ai;
            audioSource = ai.creatureVoice;
            SetNextTime();
        }

        private void Update()
        {
            if (Time.time > nextTimeToPlayAudio)
            {
                AttemptPlaySound();
                SetNextTime();
            }
        }

        private void AttemptPlaySound()
        {
            if (StartOfRound.Instance == null ||
                StartOfRound.Instance.localPlayerController == null ||
                ai == null ||
                ai.isEnemyDead)
            {
                return;
            }

            if (ai is DressGirlAI girlAi)
            {
                if (girlAi.hauntingPlayer != StartOfRound.Instance.localPlayerController ||
                    (!girlAi.staringInHaunt && !girlAi.moveTowardsDestination))
                {
                    return;
                }
            }

            Vector3 listenerPosition = StartOfRound.Instance.localPlayerController.isPlayerDead ? StartOfRound.Instance.spectateCamera.transform.position : StartOfRound.Instance.localPlayerController.transform.position;
            if (Vector3.Distance(listenerPosition, transform.position) < MAX_DIST &&
                SkinwalkerModPersistent.Instance.TryGetSample(out AudioClip sample))
            {
                audioSource.PlayOneShot(sample);
            }
        }

        private void SetNextTime()
        {
            if (SkinwalkerNetworkManager.Instance.VoiceLineFrequency.Value <= 0f)
            {
                nextTimeToPlayAudio = Time.time + 100000000f;
            }
            else
            {
                nextTimeToPlayAudio = Time.time + UnityEngine.Random.Range(PLAY_INTERVAL_MIN, PLAY_INTERVAL_MAX) / SkinwalkerNetworkManager.Instance.VoiceLineFrequency.Value;
            }
        }

        //TODO :: Needed?
        private T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            Type type = original.GetType();
            Component val = destination.AddComponent(type);
            FieldInfo[] fields = type.GetFields();
            FieldInfo[] array = fields;
            foreach (FieldInfo fieldInfo in array)
            {
                fieldInfo.SetValue(val, fieldInfo.GetValue(original));
            }
            return (T)(object)((val is T) ? val : null);
        }
    }
}
