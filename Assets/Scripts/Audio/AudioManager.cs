using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string DefaultLibraryResourcePath = "Audio/AudioLibrary";
        private const string BgmPrefsKey = "Settings.BgmVolume";
        private const string SfxPrefsKey = "Settings.SfxVolume";

        public static AudioManager Instance { get; private set; }

        [Header("라이브러리")]
        [SerializeField] private AudioLibrary library;
        [SerializeField] private bool loadLibraryFromResources = true;
        [SerializeField] private string libraryResourcePath = DefaultLibraryResourcePath;

        [Header("볼륨")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("소스")]
        [SerializeField, Min(1)] private int sfxSourcePoolSize = 8;

        [Header("UI 클릭음")]
        [SerializeField] private bool installUiButtonClickSoundBinder = true;
        [SerializeField] private UIButtonClickSoundBinder uiButtonClickSoundBinder;

        [Header("카운터 BGM")]
        [SerializeField] private bool installCounterAreaBgmDirector = true;
        [SerializeField] private CounterAreaBgmDirector counterAreaBgmDirector;

        private AudioSource bgmSource;
        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private int nextSfxSourceIndex;
        private string currentBgmId;
        private float currentBgmTrackVolume = 1f;
        private Coroutine bgmFadeRoutine;
        private bool initialized;

        public AudioLibrary Library
        {
            get => library;
            set => library = value;
        }

        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public string CurrentBgmId => currentBgmId;
        public bool IsBgmPlaying => bgmSource != null && bgmSource.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            GetOrCreate();
        }

        public static AudioManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            AudioManager found = FindFirstObjectByType<AudioManager>();

            if (found != null)
            {
                found.EnsureInitialized();
                return Instance;
            }

            GameObject owner = new GameObject(nameof(AudioManager));
            AudioManager created = owner.AddComponent<AudioManager>();
            created.EnsureInitialized();
            return created;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

            Instance = this;

            if (initialized)
            {
                return;
            }

            initialized = true;

            if (Application.isPlaying)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            LoadLibraryIfNeeded();
            CreateSourcesIfNeeded();
            CreateUiButtonClickSoundBinderIfNeeded();
            CreateCounterAreaBgmDirectorIfNeeded();
            LoadVolumesFromSettings();
            ApplyVolumes();
            PlayBootBgmIfNeeded();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayBgm(string id, bool restart = false, float fadeSeconds = -1f)
        {
            LoadLibraryIfNeeded();

            if (library == null || !library.TryGetBgm(id, out AudioLibrary.BgmTrack track))
            {
                Debug.LogWarning($"AudioManager: BGM id not found: {id}", this);
                return;
            }

            if (track.clip == null)
            {
                Debug.LogWarning($"AudioManager: BGM clip is empty: {id}", this);
                return;
            }

            if (!restart && currentBgmId == id && bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.loop = track.loop;
                currentBgmTrackVolume = track.volume;
                ApplyBgmVolume();
                return;
            }

            float resolvedFadeSeconds = ResolveFadeSeconds(track, fadeSeconds);
            currentBgmId = id;

            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
            }

            if (resolvedFadeSeconds <= 0f || bgmSource == null || !bgmSource.isPlaying)
            {
                PlayBgmImmediately(track);
                return;
            }

            bgmFadeRoutine = StartCoroutine(FadeToBgm(track, resolvedFadeSeconds));
        }

        public void StopBgm(float fadeSeconds = -1f)
        {
            if (bgmSource == null)
            {
                return;
            }

            float resolvedFadeSeconds = fadeSeconds >= 0f
                ? fadeSeconds
                : (library != null ? library.DefaultBgmFadeSeconds : 0f);

            currentBgmId = null;

            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
            }

            if (resolvedFadeSeconds <= 0f)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
                return;
            }

            bgmFadeRoutine = StartCoroutine(FadeOutBgm(resolvedFadeSeconds));
        }

        public void PlaySfx(string id)
        {
            LoadLibraryIfNeeded();

            if (library == null || !library.TryGetSfx(id, out AudioLibrary.SfxClip sfx))
            {
                Debug.LogWarning($"AudioManager: SFX id not found: {id}", this);
                return;
            }

            PlaySfx(sfx.clip, sfx.volume, sfx.pitch);
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            CreateSourcesIfNeeded();
            AudioSource source = GetNextSfxSource();
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
        }

        public void SetBgmVolume01(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyBgmVolume();
        }

        public void SetSfxVolume01(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplySfxVolume();
        }

        public void SetBgmVolume10(int volume)
        {
            SetBgmVolume01(Mathf.InverseLerp(0f, 10f, Mathf.Clamp(volume, 0, 10)));
        }

        public void SetSfxVolume10(int volume)
        {
            SetSfxVolume01(Mathf.InverseLerp(0f, 10f, Mathf.Clamp(volume, 0, 10)));
        }

        private void LoadLibraryIfNeeded()
        {
            if (library != null || !loadLibraryFromResources)
            {
                return;
            }

            string resourcePath = string.IsNullOrWhiteSpace(libraryResourcePath)
                ? DefaultLibraryResourcePath
                : libraryResourcePath;

            library = Resources.Load<AudioLibrary>(resourcePath);
        }

        private void CreateSourcesIfNeeded()
        {
            if (bgmSource == null)
            {
                bgmSource = CreateChildSource("BGM Source");
                bgmSource.loop = true;
            }

            while (sfxSources.Count < Mathf.Max(1, sfxSourcePoolSize))
            {
                sfxSources.Add(CreateChildSource($"SFX Source {sfxSources.Count + 1}"));
            }
        }

        private AudioSource CreateChildSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void CreateUiButtonClickSoundBinderIfNeeded()
        {
            if (!installUiButtonClickSoundBinder || !Application.isPlaying)
            {
                return;
            }

            if (uiButtonClickSoundBinder == null)
            {
                uiButtonClickSoundBinder = GetComponent<UIButtonClickSoundBinder>();
            }

            if (uiButtonClickSoundBinder == null)
            {
                uiButtonClickSoundBinder = gameObject.AddComponent<UIButtonClickSoundBinder>();
            }
        }

        private void CreateCounterAreaBgmDirectorIfNeeded()
        {
            if (!installCounterAreaBgmDirector || !Application.isPlaying)
            {
                return;
            }

            if (counterAreaBgmDirector == null)
            {
                counterAreaBgmDirector = GetComponent<CounterAreaBgmDirector>();
            }

            if (counterAreaBgmDirector == null)
            {
                counterAreaBgmDirector = gameObject.AddComponent<CounterAreaBgmDirector>();
            }
        }

        private void LoadVolumesFromSettings()
        {
            bgmVolume = Mathf.InverseLerp(0f, 10f, PlayerPrefs.GetInt(BgmPrefsKey, 10));
            sfxVolume = Mathf.InverseLerp(0f, 10f, PlayerPrefs.GetInt(SfxPrefsKey, 10));
        }

        private void ApplyVolumes()
        {
            ApplyBgmVolume();
            ApplySfxVolume();
        }

        private void ApplyBgmVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume * currentBgmTrackVolume;
            }
        }

        private void ApplySfxVolume()
        {
            for (int i = 0; i < sfxSources.Count; i++)
            {
                if (sfxSources[i] != null)
                {
                    sfxSources[i].volume = 1f;
                }
            }
        }

        private void PlayBootBgmIfNeeded()
        {
            if (library == null || !library.PlayBgmOnBoot || string.IsNullOrWhiteSpace(library.BootBgmId))
            {
                return;
            }

            PlayBgm(library.BootBgmId);
        }

        private void PlayBgmImmediately(AudioLibrary.BgmTrack track)
        {
            CreateSourcesIfNeeded();
            bgmSource.clip = track.clip;
            bgmSource.loop = track.loop;
            currentBgmTrackVolume = track.volume;
            ApplyBgmVolume();
            bgmSource.Play();
        }

        private IEnumerator FadeToBgm(AudioLibrary.BgmTrack track, float fadeSeconds)
        {
            float startVolume = bgmSource.volume;

            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeSeconds);
                yield return null;
            }

            bgmSource.clip = track.clip;
            bgmSource.loop = track.loop;
            bgmSource.Play();

            currentBgmTrackVolume = track.volume;
            float targetVolume = bgmVolume * track.volume;

            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeSeconds);
                yield return null;
            }

            bgmSource.volume = targetVolume;
            bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutBgm(float fadeSeconds)
        {
            float startVolume = bgmSource.volume;

            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeSeconds);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            bgmFadeRoutine = null;
        }

        private float ResolveFadeSeconds(AudioLibrary.BgmTrack track, float fadeSeconds)
        {
            if (fadeSeconds >= 0f)
            {
                return fadeSeconds;
            }

            if (track.fadeSeconds >= 0f)
            {
                return track.fadeSeconds;
            }

            return library != null ? library.DefaultBgmFadeSeconds : 0f;
        }

        private AudioSource GetNextSfxSource()
        {
            CreateSourcesIfNeeded();

            AudioSource source = sfxSources[nextSfxSourceIndex];
            nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Count;
            return source;
        }
    }
}
