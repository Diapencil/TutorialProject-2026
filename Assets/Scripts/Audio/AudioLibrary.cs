using System;
using UnityEngine;

namespace SheepSheepBurger.Audio
{
    public static class AudioCueIds
    {
        public const string GrillSizzle = "grill_sizzle";
        public const string PlaceBacon = "place_bacon";
        public const string SendPackage = "send_package";
        public const string PlaceInBox = "place_in_box";
        public const string PlaceVegetable = "place_vegetable";
        public const string PlaceCheese = "place_cheese";
        public const string SqueezeKetchup = "squeeze_ketchup";
        public const string PressPatty = "press_patty";
        public const string WrapPackage = "wrap_package";
        public const string UiClick = "ui_click";
        public const string CounterDay01 = "counter_day_01";
        public const string CounterDay02 = "counter_day_02";
        public const string CounterDay03 = "counter_day_03";
        public const string CounterDay04 = "counter_day_04";
    }

    [CreateAssetMenu(menuName = "SheepSheepBurger/Audio/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class BgmTrack
        {
            [Tooltip("코드나 AudioTrigger에서 부를 이름입니다. 예: counter, shop")]
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            public bool loop = true;
            [Tooltip("음수면 AudioLibrary의 기본 페이드 시간을 씁니다.")]
            public float fadeSeconds = -1f;
        }

        [Serializable]
        public sealed class SfxClip
        {
            [Tooltip("코드나 AudioTrigger에서 부를 이름입니다. 예: click, buy")]
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
        }

        [Header("기본값")]
        [SerializeField, Min(0f)] private float defaultBgmFadeSeconds = 0.35f;
        [SerializeField] private bool playBgmOnBoot;
        [SerializeField] private string bootBgmId;

        [Header("BGM")]
        [SerializeField] private BgmTrack[] bgmTracks = Array.Empty<BgmTrack>();

        [Header("효과음")]
        [SerializeField] private SfxClip[] sfxClips = Array.Empty<SfxClip>();

        public float DefaultBgmFadeSeconds => defaultBgmFadeSeconds;
        public bool PlayBgmOnBoot => playBgmOnBoot;
        public string BootBgmId => bootBgmId;

        public bool TryGetBgm(string id, out BgmTrack track)
        {
            track = null;

            if (string.IsNullOrWhiteSpace(id) || bgmTracks == null)
            {
                return false;
            }

            for (int i = 0; i < bgmTracks.Length; i++)
            {
                BgmTrack candidate = bgmTracks[i];

                if (candidate != null && candidate.id == id)
                {
                    track = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSfx(string id, out SfxClip sfx)
        {
            sfx = null;

            if (string.IsNullOrWhiteSpace(id) || sfxClips == null)
            {
                return false;
            }

            for (int i = 0; i < sfxClips.Length; i++)
            {
                SfxClip candidate = sfxClips[i];

                if (candidate != null && candidate.id == id)
                {
                    sfx = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
