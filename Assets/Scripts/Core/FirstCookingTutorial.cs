using System.Collections.Generic;
using SheepSheepBurger.BurgerAssembly;
using SheepSheepBurger.Counter;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private static bool isTutorialPanelOpen;

        /// <summary>안내 패널이 열린 동안에는 결과 연출을 시작하지 않도록 하는 전역 상태입니다.</summary>
        public static bool IsTutorialPanelOpen => isTutorialPanelOpen;

        private BurgerAssemblyController cooking;
        private CounterSceneCoordinator counter;
        private GameObject panel;
        private GameObject inputBlocker;
        private Text body;
        private Button continueButton;
        private RectTransform overlayRoot;
        private RectTransform dimTop;
        private RectTransform dimBottom;
        private RectTransform dimLeft;
        private RectTransform dimRight;
        private GameObject guidanceInputBlocker;
        private TutorialRaycastBlocker guidanceRaycastBlocker;
        private bool isGuidanceMaskVisible;
        private Canvas highlightCanvas;
        private GraphicRaycaster highlightRaycaster;
        private readonly List<RectTransform> guidanceTargets = new List<RectTransform>(2);
        private float pausedTimeScale = 1f;
        private bool isPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsForPlayMode()
        {
            step = Step.None;
            initialized = false;
            isTutorialPanelOpen = false;
        }

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

        public static bool ShouldShowCookingPhaseLabels
        {
            get
            {
                switch (step)
                {
                    case Step.PlacePatty:
                    case Step.PressPatty:
                    case Step.WaitFirstSide:
                    case Step.FlipPatty:
                    case Step.WaitSecondSide:
                    case Step.MovePattyToBoard:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void LateUpdate()
        {
            if (isGuidanceMaskVisible)
            {
                RefreshGuidanceMask();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Unsubscribe();
            if (ShouldSkipTutorial()) return;

            if (scene.name == "BurgerAssembly")
            {
                cooking = FindFirstObjectByType<BurgerAssemblyController>();
                if (cooking == null) return;
                cooking.SetGrillBurnProtection(true);
                SubscribeCooking();
                if (step == Step.None)
                {
                    step = Step.PlacePatty;
                    Show("첫 버거를 만들어 볼까요? 오른쪽 트레이에 담긴 패티볼을 그릴 위에 올려 주세요.");
                }
            }
            else if (scene.name == "Counter" && step == Step.Serve)
            {
                StartCoroutine(ShowServeStepAfterCounterReady());
            }
        }

        private System.Collections.IEnumerator ShowServeStepAfterCounterReady()
        {
            yield return null;

            if (step != Step.Serve)
            {
                yield break;
            }

            counter = FindFirstObjectByType<CounterSceneCoordinator>();
            if (counter == null)
            {
                yield break;
            }

            counter.BurgerServed += OnBurgerServed;
            counter.HideCurrentOrderForTutorial();
            Show(
                "포장한 버거가 준비됐어요. 버거를 손님 쪽으로 드래그해서 전달해 주세요.",
                () => counter?.RestoreCurrentOrderForTutorial());
        }

        private static bool ShouldSkipTutorial()
        {
            if (step == Step.Finished) return true;
            if (IsFirstDayOpening()) return false;
            return PlayerPrefs.GetInt(CompletedKey, 0) == 1;
        }

        private static bool IsFirstDayOpening()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.State == null) return false;
            if (gameManager.State.currentDay != 1) return false;

            DayState dayState = gameManager.State.GetOrCreateCurrentDayState();
            return dayState != null && dayState.customersServed == 0;
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
                cooking.SetGrillBurnProtection(false);
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
            isTutorialPanelOpen = false;
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
            cooking?.SetGrillBurnProtection(false);
            ResumeTime();
        }

        private void OnBurgerServed()
        {
            if (step != Step.Serve) return;
            step = Step.Finished;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();

            // 전달 직후 결과 대사 타이핑이 시작될 수 있다. 안내창이 시간을 멈추면
            // 첫 글자만 남을 수 있으므로, 안내가 떠 있는 동안 말풍선을 숨긴다.
            // 안내를 닫은 다음 프레임부터 결과 대사가 정상적으로 출력된다.
            counter?.HideCurrentOrderForTutorial();
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
            HideGuidanceMask();
            body.text = message;
            panel.SetActive(true);
            inputBlocker.SetActive(true);
            isTutorialPanelOpen = true;
            PauseTime();
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                panel.SetActive(false);
                inputBlocker.SetActive(false);
                isTutorialPanelOpen = false;
                ResumeTime();
                afterClose?.Invoke();
                if (step != Step.Finished)
                {
                    ShowGuidanceMask();
                }
            });
        }

        private void Finish()
        {
            ResumeTime();
            if (panel != null) panel.SetActive(false);
            if (inputBlocker != null) inputBlocker.SetActive(false);
            isTutorialPanelOpen = false;
            HideGuidanceMask();
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
            overlayRoot = canvasObject.transform as RectTransform;

            dimTop = CreateDimmer("TutorialDimTop", canvasObject.transform);
            dimBottom = CreateDimmer("TutorialDimBottom", canvasObject.transform);
            dimLeft = CreateDimmer("TutorialDimLeft", canvasObject.transform);
            dimRight = CreateDimmer("TutorialDimRight", canvasObject.transform);

            guidanceInputBlocker = new GameObject("TutorialGuidanceInputBlocker", typeof(RectTransform), typeof(Image), typeof(TutorialRaycastBlocker));
            guidanceInputBlocker.transform.SetParent(canvasObject.transform, false);
            var guidanceBlockerRect = (RectTransform)guidanceInputBlocker.transform;
            guidanceBlockerRect.anchorMin = Vector2.zero;
            guidanceBlockerRect.anchorMax = Vector2.one;
            guidanceBlockerRect.offsetMin = Vector2.zero;
            guidanceBlockerRect.offsetMax = Vector2.zero;
            Image guidanceBlockerImage = guidanceInputBlocker.GetComponent<Image>();
            guidanceBlockerImage.color = new Color(0f, 0f, 0f, 0.01f);
            guidanceRaycastBlocker = guidanceInputBlocker.GetComponent<TutorialRaycastBlocker>();
            guidanceRaycastBlocker.Configure(guidanceTargets);
            guidanceInputBlocker.SetActive(false);

            // 화면 전체를 덮는 투명 버튼입니다. 안내 중에는 모든 게임 입력을 가로채고,
            // 어느 곳을 누르든 현재 안내를 닫습니다.
            inputBlocker = new GameObject("TutorialInputBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
            inputBlocker.transform.SetParent(canvasObject.transform, false);
            var blockerRect = (RectTransform)inputBlocker.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            Image blockerImage = inputBlocker.GetComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.01f);
            continueButton = inputBlocker.GetComponent<Button>();
            continueButton.targetGraphic = blockerImage;

            panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            image.raycastTarget = false;
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(.5f, .5f); rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(980f, 360f);

            body = CreateText("Message", panel.transform, 32, TextAnchor.MiddleCenter);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(.5f, .5f); bodyRect.anchorMax = new Vector2(.5f, .5f);
            bodyRect.anchoredPosition = new Vector2(0f, 45f); bodyRect.sizeDelta = new Vector2(820f, 190f);

            Text hint = CreateText("ContinueHint", panel.transform, 22, TextAnchor.MiddleCenter);
            hint.text = "화면을 눌러 계속";
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(.5f, 0f);
            hintRect.anchorMax = new Vector2(.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 48f);
            hintRect.sizeDelta = new Vector2(400f, 60f);
        }

        private RectTransform CreateDimmer(string name, Transform parent)
        {
            var dimmerObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            dimmerObject.transform.SetParent(parent, false);
            Image image = dimmerObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.58f);
            image.raycastTarget = true;
            dimmerObject.SetActive(false);
            return dimmerObject.GetComponent<RectTransform>();
        }

        private void ShowGuidanceMask()
        {
            if (step == Step.Package)
            {
                HideGuidanceMask();
                return;
            }

            if (overlayRoot == null || !TryGetGuidanceTargets(guidanceTargets))
            {
                return;
            }

            isGuidanceMaskVisible = true;
            dimTop.gameObject.SetActive(true);
            dimBottom.gameObject.SetActive(true);
            dimLeft.gameObject.SetActive(true);
            dimRight.gameObject.SetActive(true);
            guidanceInputBlocker.SetActive(true);
            BringHighlightTargetForward(guidanceTargets[0]);
            RefreshGuidanceMask();
        }

        private void HideGuidanceMask()
        {
            isGuidanceMaskVisible = false;
            ClearHighlightTargetForwarding();
            if (dimTop == null) return;
            dimTop.gameObject.SetActive(false);
            dimBottom.gameObject.SetActive(false);
            dimLeft.gameObject.SetActive(false);
            dimRight.gameObject.SetActive(false);
            if (guidanceInputBlocker != null) guidanceInputBlocker.SetActive(false);
        }

        private void RefreshGuidanceMask()
        {
            if (!TryGetGuidanceTargets(guidanceTargets))
            {
                HideGuidanceMask();
                return;
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            Vector3[] corners = new Vector3[4];
            for (int targetIndex = 0; targetIndex < guidanceTargets.Count; targetIndex++)
            {
                RectTransform target = guidanceTargets[targetIndex];
                if (target == null) continue;

                target.GetWorldCorners(corners);
                Canvas targetCanvas = target.GetComponentInParent<Canvas>();
                Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? targetCanvas.worldCamera
                    : null;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[i]);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, null, out Vector2 local);
                    minX = Mathf.Min(minX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxX = Mathf.Max(maxX, local.x);
                    maxY = Mathf.Max(maxY, local.y);
                }
            }

            const float padding = 24f;
            Rect bounds = overlayRoot.rect;
            minX = Mathf.Clamp(minX - padding, bounds.xMin, bounds.xMax);
            minY = Mathf.Clamp(minY - padding, bounds.yMin, bounds.yMax);
            maxX = Mathf.Clamp(maxX + padding, bounds.xMin, bounds.xMax);
            maxY = Mathf.Clamp(maxY + padding, bounds.yMin, bounds.yMax);
            SetMaskRect(dimTop, new Rect(bounds.xMin, maxY, bounds.width, bounds.yMax - maxY));
            SetMaskRect(dimBottom, new Rect(bounds.xMin, bounds.yMin, bounds.width, minY - bounds.yMin));
            SetMaskRect(dimLeft, new Rect(bounds.xMin, minY, minX - bounds.xMin, maxY - minY));
            SetMaskRect(dimRight, new Rect(maxX, minY, bounds.xMax - maxX, maxY - minY));
            guidanceRaycastBlocker?.Configure(guidanceTargets);
        }

        private bool TryGetGuidanceTargets(List<RectTransform> targets)
        {
            targets.Clear();

            if (TryGetGuidanceTarget(out RectTransform target))
            {
                targets.Add(target);
            }

            if ((step == Step.Ketchup || step == Step.Mustard) &&
                TryFindRectByName("BoardDropArea", out RectTransform boardTarget))
            {
                targets.Add(boardTarget);
            }

            return targets.Count > 0;
        }

        private void BringHighlightTargetForward(RectTransform target)
        {
            ClearHighlightTargetForwarding();
            // 마스크가 전체 캔버스 위에 렌더링되더라도 강조 대상은 항상 밝게 보이게 합니다.
            highlightCanvas = target.gameObject.AddComponent<Canvas>();
            highlightCanvas.overrideSorting = true;
            highlightCanvas.sortingOrder = 1001;
            highlightRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
        }

        private void ClearHighlightTargetForwarding()
        {
            if (highlightRaycaster != null) Destroy(highlightRaycaster);
            if (highlightCanvas != null) Destroy(highlightCanvas);
            highlightRaycaster = null;
            highlightCanvas = null;
        }

        private bool TryGetGuidanceTarget(out RectTransform target)
        {
            target = null;
            string exactName = step switch
            {
                Step.PlacePatty => "RawPattySource",
                Step.PressPatty or Step.WaitFirstSide or Step.FlipPatty or Step.WaitSecondSide or Step.MovePattyToBoard => "CookablePatty",
                Step.BottomBun => "BottomBunTray",
                Step.Cheese => "CheeseTray",
                Step.Pickle => "PickleTray",
                Step.Ketchup => "KetchupTray",
                Step.Mustard => "MustardTray",
                Step.TopBun => "TopBunTray",
                Step.Package => "PackageButton",
                _ => null
            };

            if (step == Step.PattyOnBurger)
            {
                return TryFindRectByPrefix("LoosePatty_", out target);
            }
            if (step == Step.Serve)
            {
                BurgerDragServeHandle burger = FindFirstObjectByType<BurgerDragServeHandle>();
                target = burger != null ? burger.GetComponent<RectTransform>() : null;
                return target != null;
            }
            return !string.IsNullOrEmpty(exactName) && TryFindRectByName(exactName, out target);
        }

        private static bool TryFindRectByName(string name, out RectTransform target)
        {
            foreach (RectTransform candidate in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (candidate.gameObject.scene.IsValid() && candidate.gameObject.name == name)
                {
                    target = candidate;
                    return true;
                }
            }
            target = null;
            return false;
        }

        private static bool TryFindRectByPrefix(string prefix, out RectTransform target)
        {
            foreach (RectTransform candidate in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (candidate.gameObject.scene.IsValid() && candidate.gameObject.name.StartsWith(prefix))
                {
                    target = candidate;
                    return true;
                }
            }
            target = null;
            return false;
        }

        private static void SetMaskRect(RectTransform rect, Rect area)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = area.center;
            rect.sizeDelta = new Vector2(Mathf.Max(0f, area.width), Mathf.Max(0f, area.height));
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

    internal sealed class TutorialRaycastBlocker :
        MonoBehaviour,
        ICanvasRaycastFilter,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private List<RectTransform> allowedTargets;
        private CookingCameraSlider cameraSlider;

        public void Configure(List<RectTransform> targets)
        {
            allowedTargets = targets;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return !IsInsideAllowedTarget(screenPoint);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            FindCameraSlider()?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            FindCameraSlider()?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            FindCameraSlider()?.OnEndDrag(eventData);
        }

        private bool IsInsideAllowedTarget(Vector2 screenPoint)
        {
            if (allowedTargets == null)
            {
                return false;
            }

            for (int i = 0; i < allowedTargets.Count; i++)
            {
                RectTransform target = allowedTargets[i];
                if (target == null)
                {
                    continue;
                }

                Canvas targetCanvas = target.GetComponentInParent<Canvas>();
                Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? targetCanvas.worldCamera
                    : null;

                if (RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, targetCamera))
                {
                    return true;
                }
            }

            return false;
        }

        private CookingCameraSlider FindCameraSlider()
        {
            if (cameraSlider != null && cameraSlider.gameObject.scene.IsValid())
            {
                return cameraSlider;
            }

            cameraSlider = FindFirstObjectByType<CookingCameraSlider>();
            return cameraSlider;
        }
    }
}
