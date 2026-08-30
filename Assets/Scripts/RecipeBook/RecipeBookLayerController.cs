using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

// 프로젝트에 아직 옛날 전역 RecipeData / RecipeLayer 클래스가 남아 있어서
// 이름이 겹친다. 겹치지 않는 별칭으로 Core 쪽 타입만 콕 집어서 쓴다.
// (옛날 Assets/Scripts/RecipeData.cs / CustomerData.cs 가 정리되면 이 별칭들을 지우고
//  그냥 RecipeData / RecipeLayer 로 바꿔도 된다.)
using CoreRecipeData = SheepSheepBurger.Core.RecipeData;
using CoreRecipeLayer = SheepSheepBurger.Core.RecipeLayer;

namespace SheepSheepBurger.RecipeBook
{
    [Serializable]
    public sealed class RecipeSelectedEvent : UnityEvent<CoreRecipeData>
    {
    }

    [Serializable]
    public sealed class RecipeBookVisibilityEvent : UnityEvent<bool>
    {
    }

    /// <summary>
    /// 햄버거 도감(레시피 북) 레이어. 그림 없이 텍스트만 표시한다.
    ///
    /// [작업 방식] 씬이 아니라 "레이어 프리팹"으로 관리한다. (SettingsLayer / DayResultLayer 와 동일 패턴)
    ///  - Counter 씬 안에 RecipeBookLayer 오브젝트 하나를 두고 통째로 프리팹화한다.
    ///  - 표시 토글은 SetActive 가 아니라 CanvasGroup 의 alpha / interactable / blocksRaycasts.
    ///  - 편집은 프리팹 안(Prefab Mode)에서만. 씬 파일은 건드리지 않는다.
    ///
    /// [데이터] 스키마 원칙대로:
    ///  - 안 변하는 데이터 = RecipeData(SO) 목록  → allRecipes / SetRecipeSource()
    ///  - 변하는 상태      = GameState.unlockedRecipeIds → UnlockPredicate
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RecipeBookLayerController : MonoBehaviour
    {
        [Header("레이어 표시")]
        [SerializeField] private CanvasGroup layerCanvasGroup;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private bool createEventSystemIfMissing = true;

        [Header("레시피 데이터 (안 변하는 것)")]
        [Tooltip("도감에 실을 전체 레시피 목록.\n" +
                 "지금은 인스펙터에 직접 드래그.\n" +
                 "나중에 GameDatabase / GameManager 에서 SetRecipeSource(...) 로 넘겨도 된다.")]
        [SerializeField] private List<CoreRecipeData> allRecipes = new List<CoreRecipeData>();

        [Header("해금 상태 (변하는 것)")]
        [Tooltip("체크하면 실행 시 GameState.unlockedRecipeIds 를 해금 기준으로 사용한다.\n" +
                 "(아래 테스트 목록은 그 위에 '항상 해금'으로 더해진다.)")]
        [SerializeField] private bool useGameStateUnlocks = true;

        [Tooltip("항상 해금된 것으로 칠 RecipeData.id 목록. 테스트/디버그용.\n" +
                 "실제 해금은 퍼펙트 응대 시 GameState 에 쌓인다.")]
        [SerializeField] private List<int> testUnlockedIds = new List<int>();

        [Header("격자")]
        [SerializeField] private RectTransform entryGridPlaceholder;
        [SerializeField] private RecipeBookEntryView entryPrefab;
        [SerializeField] private TMP_Text progressText; // "3 / 12"

        [Header("상세창 (전부 텍스트)")]
        [SerializeField] private CanvasGroup detailCanvasGroup;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailIngredientsText;
        [SerializeField] private TMP_Text detailPriceText;
        [SerializeField] private Button detailCloseButton;

        [Header("도감 이벤트")]
        [Tooltip("해금된 레시피 항목을 선택했을 때 호출됩니다.")]
        public RecipeSelectedEvent onRecipeSelected = new RecipeSelectedEvent();

        [Tooltip("도감 레이어가 열릴 때(true) / 닫힐 때(false) 호출됩니다.")]
        public RecipeBookVisibilityEvent onVisibilityChanged = new RecipeBookVisibilityEvent();

        private readonly List<RecipeBookEntryView> spawnedEntries = new List<RecipeBookEntryView>();
        private Func<int, bool> unlockPredicate;

        public bool IsOpen => layerCanvasGroup != null && layerCanvasGroup.alpha > 0.5f;
        public int TotalCount => allRecipes != null ? allRecipes.Count : 0;
        public int UnlockedCount { get; private set; }

        /// <summary>
        /// "이 RecipeData.id 가 해금됐는가?" 판정 함수. 기본은 testUnlockedIds 목록 기준.
        ///
        /// GameState 연결 시 CounterSceneCoordinator 같은 곳에서 한 줄로 갈아끼운다:
        ///   recipeBook.UnlockPredicate = id => gameState.unlockedRecipeIds.Contains(id);
        /// </summary>
        public Func<int, bool> UnlockPredicate
        {
            get => unlockPredicate;
            set
            {
                unlockPredicate = value;
                if (IsOpen)
                {
                    Rebuild();
                }
            }
        }

        public void Bind(CanvasGroup boundCanvasGroup,
                         Button boundOpenButton,
                         Button boundCloseButton,
                         Button boundBackdropButton,
                         RectTransform boundEntryGridPlaceholder,
                         RecipeBookEntryView boundEntryPrefab,
                         CanvasGroup boundDetailCanvasGroup)
        {
            layerCanvasGroup = boundCanvasGroup;
            openButton = boundOpenButton;
            closeButton = boundCloseButton;
            backdropButton = boundBackdropButton;
            entryGridPlaceholder = boundEntryGridPlaceholder;
            entryPrefab = boundEntryPrefab;
            detailCanvasGroup = boundDetailCanvasGroup;
        }

        /// <summary>GameManager / GameDatabase 에서 전체 레시피 목록을 넘겨줄 때 사용.</summary>
        public void SetRecipeSource(IEnumerable<CoreRecipeData> recipes)
        {
            allRecipes = recipes != null ? new List<CoreRecipeData>(recipes) : new List<CoreRecipeData>();
            if (IsOpen)
            {
                Rebuild();
            }
        }

        /// <summary>카운터에서 새 레시피가 해금됐을 때 도감 목록과 해금 표시를 즉시 동기화합니다.</summary>
        public void RefreshUnlockedRecipe(CoreRecipeData recipe)
        {
            EnsureRecipeRegistered(recipe);
            unlockPredicate = BuildDefaultPredicate();

            if (IsOpen)
            {
                Rebuild();
            }
        }

        private void Awake()
        {
            EnsureEventSystemIfNeeded();
            EnsureBindings();
            HookButtons();
            unlockPredicate ??= BuildDefaultPredicate();
            SetDetailVisible(false);
            SetVisible(!hideOnAwake);
        }

        private void Start()
        {
            // GameManager 가 준비된 뒤(보통 Awake 순서상 여기)에서 GameState 기반 판정으로 교체.
            if (useGameStateUnlocks)
            {
                unlockPredicate = BuildDefaultPredicate();
                if (IsOpen)
                {
                    Rebuild();
                }
            }
        }

        /// <summary>
        /// 해금 판정 함수를 만든다.
        ///  - useGameStateUnlocks: GameState.unlockedRecipeIds 에 있으면 해금
        ///  - testUnlockedIds: 항상 해금 (테스트용, 위 조건에 OR 로 더해짐)
        /// </summary>
        private Func<int, bool> BuildDefaultPredicate()
        {
            List<int> alwaysUnlocked = testUnlockedIds;

            if (useGameStateUnlocks && Application.isPlaying)
            {
                SheepSheepBurger.Core.GameManager manager = SheepSheepBurger.Core.GameManager.GetOrCreate();
                if (manager != null && manager.State != null)
                {
                    SheepSheepBurger.Core.GameState state = manager.State;
                    return id => state.IsRecipeUnlocked(id) || alwaysUnlocked.Contains(id);
                }
            }

            return id => alwaysUnlocked.Contains(id);
        }

        private void OnDestroy()
        {
            UnhookButtons();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureBindings();
        }
#endif

        [ContextMenu("Open Recipe Book Layer")]
        public void Open() => SetVisible(true);

        [ContextMenu("Close Recipe Book Layer")]
        public void Close() => SetVisible(false);

        [ContextMenu("Toggle Recipe Book Layer")]
        public void Toggle() => SetVisible(!IsOpen);

        public void SetVisible(bool visible)
        {
            EnsureBindings();

            if (visible)
            {
                transform.SetAsLastSibling();
                Rebuild();
            }
            else
            {
                SetDetailVisible(false);
            }

            if (layerCanvasGroup != null)
            {
                layerCanvasGroup.alpha = visible ? 1f : 0f;
                layerCanvasGroup.interactable = visible;
                layerCanvasGroup.blocksRaycasts = visible;
            }

            onVisibilityChanged?.Invoke(visible);
        }

        [ContextMenu("Rebuild Entries")]
        public void Rebuild()
        {
            EnsureBindings();

            for (int i = 0; i < spawnedEntries.Count; i++)
            {
                if (spawnedEntries[i] != null)
                {
                    Destroy(spawnedEntries[i].gameObject);
                }
            }

            spawnedEntries.Clear();
            UnlockedCount = 0;

            if (entryPrefab == null || entryGridPlaceholder == null)
            {
                Debug.LogWarning("[RecipeBookLayerController] entryPrefab / entryGridPlaceholder 가 연결되지 않았습니다.", this);
                RefreshProgressText();
                return;
            }

            for (int i = 0; i < allRecipes.Count; i++)
            {
                CoreRecipeData recipe = allRecipes[i];
                if (recipe == null)
                {
                    continue;
                }

                bool unlocked = unlockPredicate != null && unlockPredicate(recipe.id);
                if (unlocked)
                {
                    UnlockedCount++;
                }

                RecipeBookEntryView entry = Instantiate(entryPrefab, entryGridPlaceholder);
                CoreRecipeData captured = recipe;
                entry.Bind(recipe.recipeName, unlocked, () => HandleEntrySelected(captured));
                spawnedEntries.Add(entry);
            }

            RefreshProgressText();
        }

        private void HandleEntrySelected(CoreRecipeData recipe)
        {
            if (recipe == null)
            {
                return;
            }

            ShowDetail(recipe);
            onRecipeSelected?.Invoke(recipe);
        }

        private void ShowDetail(CoreRecipeData recipe)
        {
            if (detailNameText != null) detailNameText.text = recipe.recipeName;
            if (detailPriceText != null) detailPriceText.text = $"{recipe.basePrice} c";
            if (detailIngredientsText != null) detailIngredientsText.text = BuildIngredientText(recipe);

            SetDetailVisible(true);
        }

        [ContextMenu("Hide Detail")]
        public void HideDetail() => SetDetailVisible(false);

        private void SetDetailVisible(bool visible)
        {
            if (detailCanvasGroup == null)
            {
                return;
            }

            detailCanvasGroup.alpha = visible ? 1f : 0f;
            detailCanvasGroup.interactable = visible;
            detailCanvasGroup.blocksRaycasts = visible;
        }

        private static string BuildIngredientText(CoreRecipeData recipe)
        {
            StringBuilder builder = new StringBuilder();

            if (recipe.layers != null)
            {
                for (int i = 0; i < recipe.layers.Count; i++)
                {
                    CoreRecipeLayer layer = recipe.layers[i];
                    if (layer == null || layer.ingredient == null)
                    {
                        continue;
                    }

                    builder.Append("• ").Append(layer.ingredient.ingredientName);
                    if (layer.quantity > 1)
                    {
                        builder.Append(" x").Append(layer.quantity);
                    }

                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void EnsureRecipeRegistered(CoreRecipeData recipe)
        {
            if (recipe == null)
            {
                return;
            }

            allRecipes ??= new List<CoreRecipeData>();
            for (int i = 0; i < allRecipes.Count; i++)
            {
                CoreRecipeData existing = allRecipes[i];
                if (existing != null && existing.id == recipe.id)
                {
                    return;
                }
            }

            allRecipes.Add(recipe);
            allRecipes.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.id.CompareTo(b.id);
            });
        }

        private void RefreshProgressText()
        {
            if (progressText != null)
            {
                progressText.text = $"{UnlockedCount} / {TotalCount}";
            }
        }

        private void EnsureBindings()
        {
            if (layerCanvasGroup == null)
            {
                layerCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void EnsureEventSystemIfNeeded()
        {
            if (!createEventSystemIfMissing || EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystemObject);
        }

        private void HookButtons()
        {
            HookButton(openButton, Toggle);
            HookButton(closeButton, Close);
            HookButton(backdropButton, Close);
            HookButton(detailCloseButton, HideDetail);
        }

        private void UnhookButtons()
        {
            UnhookButton(openButton, Toggle);
            UnhookButton(closeButton, Close);
            UnhookButton(backdropButton, Close);
            UnhookButton(detailCloseButton, HideDetail);
        }

        private static void HookButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnhookButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
        }
    }
}
