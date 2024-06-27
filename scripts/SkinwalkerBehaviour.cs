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
            float random = UnityEngine.Random.Range(PLAY_INTERVAL_MIN, PLAY_INTERVAL_MAX);
            nextTimeToPlayAudio = Time.time + random / SkinwalkerNetworkManager.Instance.VoiceLineFrequency.Value;
        }

        //TODO :: Needed?
        private T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            Type type = original.GetType();
            Component addedComponent = destination.AddComponent(type);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo fieldInfo in fields)
            {
                fieldInfo.SetValue(addedComponent, fieldInfo.GetValue(original));
            }
            return addedComponent is T converted ? converted : null;
        }
    }
}
