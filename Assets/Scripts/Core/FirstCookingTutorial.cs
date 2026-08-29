using SheepSheepBurger.BurgerAssembly;
using SheepSheepBurger.Counter;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AssemblyIngredientType = SheepSheepBurger.BurgerAssembly.IngredientType;

namespace SheepSheepBurger.Core
{
    /// <summary>
    /// 첫 버거 제작의 행동 순서를 안내하는 씬 간 튜토리얼입니다.
    /// 안내창을 띄운 동안 Time.timeScale을 0으로 유지해 조리 타이머와 패티 굽기를 멈춥니다.
    /// </summary>
    public sealed class FirstCookingTutorial : MonoBehaviour
    {
        private const string CompletedKey = "FirstCookingTutorial.Completed";
        private enum Step
        {
            None, PlacePatty, PressPatty, WaitFirstSide, FlipPatty, WaitSecondSide,
            MovePattyToBoard, BottomBun, PattyOnBurger, Cheese, Pickle, Ketchup,
            Mustard, TopBun, Package, Serve, Finished
        }

        private static Step step;
        private static bool initialized;
        private BurgerAssemblyController cooking;
        private CounterSceneCoordinator counter;
        private GameObject panel;
        private Text body;
        private float pausedTimeScale = 1f;
        private bool isPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (initialized) return;
            initialized = true;
            var host = new GameObject(nameof(FirstCookingTutorial));
            DontDestroyOnLoad(host);
            host.AddComponent<FirstCookingTutorial>();
        }

        /// <summary>개발 중 첫 조리 튜토리얼 완료 기록을 지우고 다음 요리 씬 진입에서 다시 시작합니다.</summary>
        public static void ResetCompletionForDevelopment()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();
            step = Step.None;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Unsubscribe();
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1 || step == Step.Finished) return;

            if (scene.name == "BurgerAssembly")
            {
                cooking = FindFirstObjectByType<BurgerAssemblyController>();
                if (cooking == null) return;
                SubscribeCooking();
                if (step == Step.None)
                {
                    step = Step.PlacePatty;
                    Show("첫 버거를 만들어 볼까요? 왼쪽 재료함의 패티를 그릴 위에 올려 주세요.");
                }
            }
            else if (scene.name == "Counter" && step == Step.Serve)
            {
                counter = FindFirstObjectByType<CounterSceneCoordinator>();
                if (counter == null) return;
                counter.BurgerServed += OnBurgerServed;
                Show("포장한 버거가 준비됐어요. 버거를 손님 쪽으로 드래그해서 전달해 주세요.");
            }
        }

        private void SubscribeCooking()
        {
            cooking.GrillIngredientPlaced += OnGrillIngredientPlaced;
            cooking.GrillPhaseChanged += OnGrillPhaseChanged;
            cooking.BoardIngredientPlaced += OnBoardIngredientPlaced;
            cooking.SauceApplied += OnSauceApplied;
            if (cooking.PackagingController != null)
                cooking.PackagingController.Packaged += OnPackaged;
            else
                StartCoroutine(SubscribePackagingWhenReady());
        }

        private System.Collections.IEnumerator SubscribePackagingWhenReady()
        {
            while (cooking != null && cooking.PackagingController == null) yield return null;
            if (cooking != null) cooking.PackagingController.Packaged += OnPackaged;
        }

        private void Unsubscribe()
        {
            if (cooking != null)
            {
                cooking.GrillIngredientPlaced -= OnGrillIngredientPlaced;
                cooking.GrillPhaseChanged -= OnGrillPhaseChanged;
                cooking.BoardIngredientPlaced -= OnBoardIngredientPlaced;
                cooking.SauceApplied -= OnSauceApplied;
                if (cooking.PackagingController != null) cooking.PackagingController.Packaged -= OnPackaged;
            }
            if (counter != null) counter.BurgerServed -= OnBurgerServed;
            cooking = null;
            counter = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ResumeTime();
        }

        private void OnGrillIngredientPlaced(AssemblyIngredientType type)
        {
            if (step == Step.PlacePatty && type == AssemblyIngredientType.Patty)
            {
                step = Step.PressPatty;
                Show("패티를 한 번 눌러 조리를 시작하세요.");
            }
        }

        private void OnGrillPhaseChanged(AssemblyIngredientType type, PattyGrillPhase phase)
        {
            if (type != AssemblyIngredientType.Patty) return;
            if (step == Step.PressPatty && phase == PattyGrillPhase.CookingSide1)
            {
                step = Step.WaitFirstSide;
                Show("첫 면이 익을 때까지 기다려 주세요.");
            }
            else if (step == Step.WaitFirstSide && phase == PattyGrillPhase.ReadyToFlip)
            {
                step = Step.FlipPatty;
                Show("첫 면이 익었습니다. 패티를 눌러 뒤집으세요.");
            }
            else if (step == Step.FlipPatty && phase == PattyGrillPhase.CookingSide2)
            {
                step = Step.WaitSecondSide;
                Show("두 번째 면도 익을 때까지 기다려 주세요.");
            }
            else if (step == Step.WaitSecondSide && phase == PattyGrillPhase.Done)
            {
                step = Step.MovePattyToBoard;
                Show("패티가 다 익었어요. 오른쪽 도마로 드래그해 옮겨 주세요.");
            }
        }

        private void OnBoardIngredientPlaced(AssemblyIngredientType type)
        {
            if (step == Step.MovePattyToBoard && type == AssemblyIngredientType.Patty) Advance(Step.BottomBun, "이제 하단 번을 도마에 놓아 버거의 바닥을 만들어 주세요.");
            else if (step == Step.BottomBun && type == AssemblyIngredientType.BunBottom) Advance(Step.PattyOnBurger, "구운 패티를 하단 번 위에 쌓아 주세요.");
            else if (step == Step.PattyOnBurger && type == AssemblyIngredientType.Patty) Advance(Step.Cheese, "패티 위에 치즈를 올려 주세요.");
            else if (step == Step.Cheese && type == AssemblyIngredientType.ToppingCheese) Advance(Step.Pickle, "치즈 위에 피클을 올려 주세요.");
            else if (step == Step.Pickle && type == AssemblyIngredientType.ToppingPickle) Advance(Step.Ketchup, "케첩 소스를 버거 위에 뿌려 주세요.");
            else if (step == Step.TopBun && type == AssemblyIngredientType.BunTop) Advance(Step.Package, "버거가 완성됐어요. 오른쪽 포장 구역으로 옮겨 포장해 주세요.");
        }

        private void OnSauceApplied(AssemblyIngredientType type)
        {
            if (step == Step.Ketchup && type == AssemblyIngredientType.SauceKetchup) Advance(Step.Mustard, "이번에는 머스타드 소스를 뿌려 주세요.");
            else if (step == Step.Mustard && type == AssemblyIngredientType.SauceMustard) Advance(Step.TopBun, "마지막으로 상단 번을 올려 버거를 완성하세요.");
        }

        private void OnPackaged()
        {
            if (step != Step.Package) return;
            step = Step.Serve;
            ResumeTime();
        }

        private void OnBurgerServed()
        {
            if (step != Step.Serve) return;
            step = Step.Finished;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            Show("첫 버거를 손님에게 전달했습니다! 이제부터는 직접 주문을 완성해 보세요.", Finish);
        }

        private void Advance(Step next, string message)
        {
            step = next;
            Show(message);
        }

        private void Show(string message, System.Action afterClose = null)
        {
            EnsurePanel();
            body.text = message;
            panel.SetActive(true);
            PauseTime();
            Button button = panel.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                panel.SetActive(false);
                ResumeTime();
                afterClose?.Invoke();
            });
        }

        private void Finish()
        {
            ResumeTime();
            if (panel != null) panel.SetActive(false);
        }

        private void PauseTime()
        {
            if (isPaused) return;
            pausedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;
        }

        private void ResumeTime()
        {
            if (!isPaused) return;
            Time.timeScale = pausedTimeScale;
            isPaused = false;
        }

        private void EnsurePanel()
        {
            if (panel != null) return;
            var canvasObject = new GameObject("FirstCookingTutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

            panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(.5f, .5f); rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(980f, 360f);

            body = CreateText("Message", panel.transform, 32, TextAnchor.MiddleCenter);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(.5f, .5f); bodyRect.anchorMax = new Vector2(.5f, .5f);
            bodyRect.anchoredPosition = new Vector2(0f, 45f); bodyRect.sizeDelta = new Vector2(820f, 190f);

            var buttonObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.anchorMin = new Vector2(.5f, 0f); buttonRect.anchorMax = new Vector2(.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 48f); buttonRect.sizeDelta = new Vector2(250f, 72f);
            buttonObject.GetComponent<Image>().color = new Color(.96f, .62f, .18f, 1f);
            Text label = CreateText("Label", buttonObject.transform, 26, TextAnchor.MiddleCenter);
            label.text = "확인";
            var labelRect = label.rectTransform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, int size, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            // Unity 6부터 기본 내장 폰트 리소스 이름은 Arial.ttf가 아니라 LegacyRuntime.ttf입니다.
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
