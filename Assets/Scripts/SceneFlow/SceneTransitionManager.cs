using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.SceneFlow
{
    /// <summary>
    /// 화면 위에 얇은 전환 레이어를 띄워 씬 로드를 부드럽게 보이게 하는 공용 매니저입니다.
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private const float DefaultSlideDuration = 0.35f;
        private const float DefaultFadeDuration = 0.28f;
        private const int SortingOrder = 32000;

        private static SceneTransitionManager instance;

        [SerializeField] private Color transitionColor = new Color(0.12f, 0.08f, 0.05f, 1f);

        private Canvas canvas;
        private RectTransform panelRect;
        private Image panelImage;
        private bool isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static void LoadSceneSlideLeft(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            GetOrCreate().StartTransition(sceneName, TransitionMode.SlideLeft);
        }

        public static void LoadSceneFade(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            GetOrCreate().StartTransition(sceneName, TransitionMode.Fade);
        }

        private static SceneTransitionManager GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            var host = new GameObject(nameof(SceneTransitionManager));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<SceneTransitionManager>();
            instance.EnsureCanvas();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
        }

        private void StartTransition(string sceneName, TransitionMode mode)
        {
            if (isTransitioning)
            {
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName, mode));
        }

        private IEnumerator TransitionRoutine(string sceneName, TransitionMode mode)
        {
            isTransitioning = true;
            EnsureCanvas();
            canvas.gameObject.SetActive(true);

            if (mode == TransitionMode.Fade)
            {
                yield return FadeRoutine(0f, 1f, DefaultFadeDuration);
                SceneManager.LoadScene(sceneName);
                yield return null;
                yield return FadeRoutine(1f, 0f, DefaultFadeDuration);
            }
            else
            {
                float width = Mathf.Max(Screen.width, 1920f);
                panelImage.color = transitionColor;
                SetPanelAlpha(1f);
                panelRect.anchoredPosition = new Vector2(width, 0f);
                yield return SlideRoutine(width, 0f, DefaultSlideDuration);
                SceneManager.LoadScene(sceneName);
                yield return null;
                yield return SlideRoutine(0f, -width, DefaultSlideDuration);
            }

            canvas.gameObject.SetActive(false);
            isTransitioning = false;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            panelRect.anchoredPosition = Vector2.zero;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetPanelAlpha(Mathf.Lerp(from, to, Smooth(t)));
                yield return null;
            }

            SetPanelAlpha(to);
        }

        private IEnumerator SlideRoutine(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                panelRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, Smooth(t)), 0f);
                yield return null;
            }

            panelRect.anchoredPosition = new Vector2(toX, 0f);
        }

        private void EnsureCanvas()
        {
            if (canvas != null && panelRect != null && panelImage != null)
            {
                return;
            }

            var canvasObject = new GameObject("SceneTransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("TransitionPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(2300f, 1300f);
            panelRect.anchoredPosition = Vector2.zero;

            panelImage = panelObject.GetComponent<Image>();
            panelImage.color = transitionColor;
            panelImage.raycastTarget = true;
            canvasObject.SetActive(false);
        }

        private void SetPanelAlpha(float alpha)
        {
            Color color = panelImage.color;
            color.a = alpha;
            panelImage.color = color;
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private enum TransitionMode
        {
            SlideLeft,
            Fade
        }
    }
}
