using System;
using SheepSheepBurger.Audio;
using SheepSheepBurger.SceneFlow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SheepSheepBurger.Settings
{
    [Serializable]
    public sealed class SettingsVolumeChangedEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public sealed class SettingsNormalizedVolumeChangedEvent : UnityEvent<float>
    {
    }

    [DisallowMultipleComponent]
    public sealed class SettingsLayerController : MonoBehaviour
    {
        public const int MinVolume = 0;
        public const int MaxVolume = 10;

        [Header("레이어 표시")]
        [SerializeField] private CanvasGroup layerCanvasGroup;
        [SerializeField] private Button settingsToggleButton;
        [SerializeField] private Button exitToStartButton;
        [SerializeField] private string startSceneName = "StartScene";
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private bool createEventSystemIfMissing = true;

        [Header("BGM")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmValueText;
        [SerializeField, Range(MinVolume, MaxVolume)] private int defaultBgmVolume = MaxVolume;

        [Header("효과음")]
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueText;
        [SerializeField, Range(MinVolume, MaxVolume)] private int defaultSfxVolume = MaxVolume;

        [Header("저장")]
        [SerializeField] private bool persistValues = true;
        [SerializeField] private string bgmPrefsKey = "Settings.BgmVolume";
        [SerializeField] private string sfxPrefsKey = "Settings.SfxVolume";

        [Header("볼륨 이벤트")]
        [Tooltip("BGM 볼륨이 0~10 정수로 바뀔 때 호출됩니다.")]
        public SettingsVolumeChangedEvent onBgmVolumeChanged = new SettingsVolumeChangedEvent();

        [Tooltip("BGM 볼륨이 0~1 값으로 바뀔 때 호출됩니다.")]
        public SettingsNormalizedVolumeChangedEvent onBgmNormalizedVolumeChanged =
            new SettingsNormalizedVolumeChangedEvent();

        [Tooltip("효과음 볼륨이 0~10 정수로 바뀔 때 호출됩니다.")]
        public SettingsVolumeChangedEvent onSfxVolumeChanged = new SettingsVolumeChangedEvent();

        [Tooltip("효과음 볼륨이 0~1 값으로 바뀔 때 호출됩니다.")]
        public SettingsNormalizedVolumeChangedEvent onSfxNormalizedVolumeChanged =
            new SettingsNormalizedVolumeChangedEvent();

        private int bgmVolume = MaxVolume;
        private int sfxVolume = MaxVolume;

        public bool IsOpen => layerCanvasGroup != null && layerCanvasGroup.alpha > 0.5f;
        public int BgmVolume => bgmVolume;
        public int SfxVolume => sfxVolume;
        public float BgmNormalizedVolume => NormalizeVolume(bgmVolume);
        public float SfxNormalizedVolume => NormalizeVolume(sfxVolume);

        public void Bind(CanvasGroup boundCanvasGroup,
                         Button boundSettingsToggleButton,
                         Slider boundBgmSlider,
                         TMP_Text boundBgmValueText,
                         Slider boundSfxSlider,
                         TMP_Text boundSfxValueText,
                         Button boundExitToStartButton = null)
        {
            layerCanvasGroup = boundCanvasGroup;
            settingsToggleButton = boundSettingsToggleButton;
            bgmSlider = boundBgmSlider;
            bgmValueText = boundBgmValueText;
            sfxSlider = boundSfxSlider;
            sfxValueText = boundSfxValueText;
            exitToStartButton = boundExitToStartButton;
        }

        private void Awake()
        {
            EnsureEventSystemIfNeeded();
            EnsureBindings();
            ConfigureSliders();
            HookButtons();
            HookSliders();
            LoadVolumes();
            SetVisible(!hideOnAwake);
        }

        private void EnsureEventSystemIfNeeded()
        {
            if (!createEventSystemIfMissing || EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private void OnDestroy()
        {
            UnhookButtons();
            UnhookSliders();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            defaultBgmVolume = ClampVolume(defaultBgmVolume);
            defaultSfxVolume = ClampVolume(defaultSfxVolume);
            ConfigureSliders();
            RefreshValueTexts();
        }
#endif

        [ContextMenu("Open Settings Layer")]
        public void Open()
        {
            SetVisible(true);
        }

        [ContextMenu("Close Settings Layer")]
        public void Close()
        {
            SetVisible(false);
        }

        [ContextMenu("Toggle Settings Layer")]
        public void Toggle()
        {
            SetVisible(!IsOpen);
        }

        public void ExitToStartScene()
        {
            if (string.IsNullOrWhiteSpace(startSceneName))
            {
                Debug.LogWarning("SettingsLayerController: Start scene name is empty.", this);
                return;
            }

            SetVisible(false);
            SceneTransitionManager.LoadSceneFade(startSceneName);
        }

        public void SetVisible(bool visible)
        {
            EnsureBindings();

            if (visible)
            {
                transform.SetAsLastSibling();
            }

            if (layerCanvasGroup == null)
            {
                return;
            }

            layerCanvasGroup.alpha = visible ? 1f : 0f;
            layerCanvasGroup.interactable = visible;
            layerCanvasGroup.blocksRaycasts = visible;
        }

        public void SetBgmVolume(float value)
        {
            SetBgmVolume(Mathf.RoundToInt(value));
        }

        public void SetBgmVolume(int value)
        {
            bgmVolume = SetVolume(value,
                                  bgmVolume,
                                  bgmSlider,
                                  bgmValueText,
                                  bgmPrefsKey,
                                  onBgmVolumeChanged,
                                  onBgmNormalizedVolumeChanged);
            AudioManager.GetOrCreate().SetBgmVolume10(bgmVolume);
        }

        public void SetSfxVolume(float value)
        {
            SetSfxVolume(Mathf.RoundToInt(value));
        }

        public void SetSfxVolume(int value)
        {
            sfxVolume = SetVolume(value,
                                  sfxVolume,
                                  sfxSlider,
                                  sfxValueText,
                                  sfxPrefsKey,
                                  onSfxVolumeChanged,
                                  onSfxNormalizedVolumeChanged);
            AudioManager.GetOrCreate().SetSfxVolume10(sfxVolume);
        }

        private void EnsureBindings()
        {
            if (layerCanvasGroup == null)
            {
                layerCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void ConfigureSliders()
        {
            ConfigureSlider(bgmSlider);
            ConfigureSlider(sfxSlider);
        }

        private void HookSliders()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
                bgmSlider.onValueChanged.AddListener(HandleBgmSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
                sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
            }
        }

        private void HookButtons()
        {
            if (settingsToggleButton != null)
            {
                settingsToggleButton.onClick.RemoveListener(Toggle);
                settingsToggleButton.onClick.AddListener(Toggle);
            }

            if (exitToStartButton != null)
            {
                exitToStartButton.onClick.RemoveListener(ExitToStartScene);
                exitToStartButton.onClick.AddListener(ExitToStartScene);
            }
        }

        private void UnhookButtons()
        {
            if (settingsToggleButton != null)
            {
                settingsToggleButton.onClick.RemoveListener(Toggle);
            }

            if (exitToStartButton != null)
            {
                exitToStartButton.onClick.RemoveListener(ExitToStartScene);
            }
        }

        private void UnhookSliders()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
            }
        }

        private void LoadVolumes()
        {
            int savedBgmVolume = persistValues
                ? PlayerPrefs.GetInt(bgmPrefsKey, defaultBgmVolume)
                : defaultBgmVolume;
            int savedSfxVolume = persistValues
                ? PlayerPrefs.GetInt(sfxPrefsKey, defaultSfxVolume)
                : defaultSfxVolume;

            SetBgmVolume(savedBgmVolume);
            SetSfxVolume(savedSfxVolume);
        }

        private void HandleBgmSliderChanged(float value)
        {
            SetBgmVolume(value);
        }

        private void HandleSfxSliderChanged(float value)
        {
            SetSfxVolume(value);
        }

        private int SetVolume(int requestedValue,
                              int currentValue,
                              Slider slider,
                              TMP_Text valueText,
                              string prefsKey,
                              SettingsVolumeChangedEvent intEvent,
                              SettingsNormalizedVolumeChangedEvent normalizedEvent)
        {
            int clampedValue = ClampVolume(requestedValue);

            if (slider != null)
            {
                slider.SetValueWithoutNotify(clampedValue);
            }

            if (valueText != null)
            {
                valueText.text = clampedValue.ToString();
            }

            if (persistValues && !string.IsNullOrEmpty(prefsKey))
            {
                PlayerPrefs.SetInt(prefsKey, clampedValue);
                PlayerPrefs.Save();
            }

            if (currentValue != clampedValue)
            {
                intEvent?.Invoke(clampedValue);
                normalizedEvent?.Invoke(NormalizeVolume(clampedValue));
            }

            return clampedValue;
        }

        private void RefreshValueTexts()
        {
            if (bgmValueText != null)
            {
                bgmValueText.text = ClampVolume(defaultBgmVolume).ToString();
            }

            if (sfxValueText != null)
            {
                sfxValueText.text = ClampVolume(defaultSfxVolume).ToString();
            }
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = MinVolume;
            slider.maxValue = MaxVolume;
            slider.wholeNumbers = true;
        }

        private static int ClampVolume(int value)
        {
            return Mathf.Clamp(value, MinVolume, MaxVolume);
        }

        private static float NormalizeVolume(int value)
        {
            return Mathf.InverseLerp(MinVolume, MaxVolume, ClampVolume(value));
        }
    }
}
