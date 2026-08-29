using UnityEngine;

namespace SheepSheepBurger.Audio
{
    public enum AudioTriggerType
    {
        Bgm,
        Sfx
    }

    public sealed class AudioTrigger : MonoBehaviour
    {
        [SerializeField] private AudioTriggerType triggerType = AudioTriggerType.Sfx;
        [SerializeField] private string audioId;
        [SerializeField] private bool playOnStart;

        [Header("BGM")]
        [SerializeField] private bool restartBgm;
        [SerializeField] private float bgmFadeSeconds = -1f;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (string.IsNullOrWhiteSpace(audioId))
            {
                Debug.LogWarning("AudioTrigger: audioId is empty.", this);
                return;
            }

            AudioManager audioManager = AudioManager.GetOrCreate();

            if (triggerType == AudioTriggerType.Bgm)
            {
                audioManager.PlayBgm(audioId, restartBgm, bgmFadeSeconds);
            }
            else
            {
                audioManager.PlaySfx(audioId);
            }
        }

        public void StopBgm()
        {
            AudioManager.GetOrCreate().StopBgm(bgmFadeSeconds);
        }
    }
}
