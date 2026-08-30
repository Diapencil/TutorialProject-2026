using System;
using SheepSheepBurger.SceneFlow;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.Start
{
    [DisallowMultipleComponent]
    public sealed class StartSceneController : MonoBehaviour
    {
        [Header("Scene Transition")]
        [SerializeField] private string counterSceneName = "Counter";

        [Header("Editable Copy")]
        [SerializeField] private string gameTitle = "SHEEP SHEEP BURGER";
        [SerializeField] private string subtitle = "오늘도 맛있는 하루를 시작해 볼까요?";
        [SerializeField] private string startButtonLabel = "게임 시작";

        [Header("Scene References")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text startButtonText;

        private bool isLoading;

        public string CounterSceneName => counterSceneName;

        public string GameTitle => gameTitle;

        public void Configure(
            Button boundStartButton,
            TMP_Text boundTitleText,
            TMP_Text boundSubtitleText,
            TMP_Text boundStartButtonText)
        {
            startButton = boundStartButton != null
                ? boundStartButton
                : throw new ArgumentNullException(nameof(boundStartButton));
            titleText = boundTitleText != null
                ? boundTitleText
                : throw new ArgumentNullException(nameof(boundTitleText));
            subtitleText = boundSubtitleText != null
                ? boundSubtitleText
                : throw new ArgumentNullException(nameof(boundSubtitleText));
            startButtonText = boundStartButtonText != null
                ? boundStartButtonText
                : throw new ArgumentNullException(nameof(boundStartButtonText));
            RefreshCopy();
        }

        private void OnEnable()
        {
            RefreshCopy();
            if (Application.isPlaying && startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
                startButton.onClick.AddListener(StartGame);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshCopy();
        }
#endif

        public void StartGame()
        {
            if (isLoading)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(counterSceneName))
            {
                Debug.LogError("[StartScene] Counter scene name is empty.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(counterSceneName))
            {
                Debug.LogError(
                    $"[StartScene] Scene '{counterSceneName}' is not enabled in Build Settings.",
                    this);
                return;
            }

            isLoading = true;
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            SceneTransitionManager.LoadSceneSlideLeft(counterSceneName);
        }

        private void RefreshCopy()
        {
            if (titleText != null)
            {
                titleText.text = gameTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
            }

            if (startButtonText != null)
            {
                startButtonText.text = startButtonLabel;
            }
        }
    }
}
