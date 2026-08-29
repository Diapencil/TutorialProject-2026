using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.Audio
{
    public static class AudioSceneFilter
    {
        public static bool IsExcludedScene(string sceneName, string[] excludedSceneNames)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || excludedSceneNames == null)
            {
                return false;
            }

            for (int i = 0; i < excludedSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, excludedSceneNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsActiveSceneExcluded(string[] excludedSceneNames)
        {
            return IsExcludedScene(SceneManager.GetActiveScene().name, excludedSceneNames);
        }
    }

    [DisallowMultipleComponent]
    public sealed class UIButtonClickSoundBinder : MonoBehaviour
    {
        [SerializeField] private string clickSfxId = AudioCueIds.UiClick;
        [SerializeField] private string[] excludedSceneNames = { "Cooking", "BurgerAssembly" };
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

        private float nextRefreshTime;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BindExistingButtons();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
            BindExistingButtons();
        }

        public void BindExistingButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < buttons.Length; i++)
            {
                BindButton(buttons[i]);
            }
        }

        private void BindButton(Button button)
        {
            if (button == null ||
                AudioSceneFilter.IsExcludedScene(button.gameObject.scene.name, excludedSceneNames))
            {
                return;
            }

            UIButtonClickSoundEmitter emitter = button.GetComponent<UIButtonClickSoundEmitter>();

            if (emitter == null)
            {
                emitter = button.gameObject.AddComponent<UIButtonClickSoundEmitter>();
            }

            emitter.Configure(clickSfxId, excludedSceneNames);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(BindAfterSceneLoad());
        }

        private IEnumerator BindAfterSceneLoad()
        {
            yield return null;
            BindExistingButtons();
        }
    }
}
