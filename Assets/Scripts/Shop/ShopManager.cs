// 상점 화면 전체를 총괄한다. 탭 전환, 슬롯 채우기, 구매 처리, 빚 상환, 상단 HUD 갱신.
using System.Collections.Generic;
using System.Globalization;
using SheepSheepBurger.Counter;
using SheepSheepBurger.Core;
using SheepSheepBurger.SceneFlow;
using SheepSheepBurger.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.Shop
{
    public class ShopManager : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private IngredientData[] allIngredients;
        [SerializeField] private UpgradeData[] allUpgrades;
        [SerializeField] private DecorationData[] allDecorations;

        [Header("탭 버튼")]
        [SerializeField] private Button toppingTabButton;
        [SerializeField] private Button upgradeTabButton;
        [SerializeField] private Button decorationTabButton;
        [SerializeField] private Button debtTabButton;

        [Header("패널")]
        [Tooltip("토핑·업&수리·장식 탭이 공용으로 쓰는 그리드 패널.")]
        [SerializeField] private GameObject gridPanel;

        [Tooltip("D-day(빚 상환) 탭 전용 패널.")]
        [SerializeField] private GameObject debtPanel;

        [Header("슬롯")]
        [SerializeField] private ScrollRect slotScrollRect;
        [SerializeField] private Transform slotParent;
        [SerializeField] private ShopSlotUI slotPrefab;
        [SerializeField] private int slotCount = 4;
        [SerializeField] private string counterSceneName = "Counter";

        [Header("HUD")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text dDayText;
        [SerializeField] private TMP_Text messageText;

        [Header("빚 상환")]
        [SerializeField] private TMP_Text debtRemainingText;
        [SerializeField] private TMP_InputField repayInputField;
        [SerializeField] private Button repayConfirmButton;

        [Header("문구")]
        [SerializeField] private string insufficientGoldMessage = "캐이나인이 부족합니다";
        [SerializeField] private string invalidAmountMessage = "올바른 금액을 입력하세요";
        [SerializeField] private string notEnoughGoldForRepayMessage = "보유 금액이 부족합니다";
        [SerializeField] private string debtClearedMessage = "빚을 모두 갚았습니다!";
        [SerializeField] private string noGameManagerMessage = "GameManager를 찾을 수 없습니다";
        [SerializeField] private string underConstructionLabel = "공사중";

        // TODO(기획확인): 슬롯 라벨/잔여 부채 표기 형식이 스토리보드에 없어 임시 형식을 쓴다.
        [SerializeField] private string upgradeLabelFormat = "{0} Lv.{1}/{2}";
        [SerializeField] private string dDayFormat = "D-{0}";

        private readonly List<ShopSlotUI> slots = new List<ShopSlotUI>();
        private ShopTabType currentTab = ShopTabType.Topping;

        /// <summary>GameManager가 없으면 null. 모든 접근부에서 방어한다.</summary>
        private GameState State => SheepSheepBurger.Core.GameManager.Instance != null
            ? SheepSheepBurger.Core.GameManager.Instance.State
            : null;

        private void Awake()
        {
            ResolveCatalog();

            if (Application.isPlaying)
            {
                SheepSheepBurger.Core.GameManager.GetOrCreate();
            }
        }

        private void Start()
        {
            BuildSlotPool();
            HookButtons();
            SelectTab(ShopTabType.Topping);
        }

        private void ResolveCatalog()
        {
            if (catalog == null)
            {
                catalog = ShopCatalog.LoadDefault();
            }

            if (catalog == null)
            {
                return;
            }

            allIngredients = catalog.Ingredients;
            allUpgrades = catalog.Upgrades;
            allDecorations = catalog.Decorations;
        }

        private void BuildSlotPool()
        {
            if (slotPrefab == null || slotParent == null)
            {
                return;
            }

            // 탭을 바꿀 때마다 파괴/생성하지 않고 미리 만들어 두고 재사용한다.
            int poolSize = GetRequiredSlotPoolSize();
            for (int i = 0; i < poolSize; i++)
            {
                ShopSlotUI slot = Instantiate(slotPrefab, slotParent);
                slot.gameObject.SetActive(false);
                slots.Add(slot);
            }
        }

        private void HookButtons()
        {
            AddTabListener(toppingTabButton, ShopTabType.Topping);
            AddTabListener(upgradeTabButton, ShopTabType.Upgrade);
            AddTabListener(decorationTabButton, ShopTabType.Decoration);
            AddTabListener(debtTabButton, ShopTabType.Debt);

            if (repayConfirmButton != null)
            {
                repayConfirmButton.onClick.RemoveAllListeners();
                repayConfirmButton.onClick.AddListener(OnRepayConfirm);
            }
        }

        private void AddTabListener(Button button, ShopTabType tab)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectTab(tab));
        }

        public void SelectTab(ShopTabType tab)
        {
            currentTab = tab;

            bool isDebtTab = tab == ShopTabType.Debt;

            if (gridPanel != null)
            {
                gridPanel.SetActive(!isDebtTab);
            }

            if (debtPanel != null)
            {
                debtPanel.SetActive(isDebtTab);
            }

            // 선택된 탭 버튼은 눌리지 않게 해서 시각적으로 구분한다.
            SetTabInteractable(toppingTabButton, tab != ShopTabType.Topping);
            SetTabInteractable(upgradeTabButton, tab != ShopTabType.Upgrade);
            SetTabInteractable(decorationTabButton, tab != ShopTabType.Decoration);
            SetTabInteractable(debtTabButton, tab != ShopTabType.Debt);

            if (State == null)
            {
                ShowMessage(noGameManagerMessage);
            }
            else
            {
                ClearMessage();
            }

            if (isDebtTab)
            {
                RefreshDebtPanel();
            }
            else
            {
                RefreshGrid();
                ResetSlotScroll();
            }

            RefreshHud();
        }

        private int GetRequiredSlotPoolSize()
        {
            int result = Mathf.Max(0, slotCount);
            result = Mathf.Max(result, GetToppingShopItemCount());
            result = Mathf.Max(result, allUpgrades != null ? allUpgrades.Length : 0);
            result = Mathf.Max(result, allDecorations != null ? allDecorations.Length : 0);
            return result;
        }

        private int GetToppingShopItemCount()
        {
            int count = 0;

            if (allIngredients == null)
            {
                return count;
            }

            for (int i = 0; i < allIngredients.Length; i++)
            {
                IngredientData data = allIngredients[i];
                if (data != null && !data.isDefaultUnlocked)
                {
                    count++;
                }
            }

            return count;
        }

        private void ResetSlotScroll()
        {
            if (slotScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            slotScrollRect.StopMovement();
            slotScrollRect.horizontalNormalizedPosition = 0f;

            if (slotScrollRect.content != null)
            {
                Vector2 position = slotScrollRect.content.anchoredPosition;
                position.x = 0f;
                slotScrollRect.content.anchoredPosition = position;
            }
        }

        private static void SetTabInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void RefreshGrid()
        {
            switch (currentTab)
            {
                case ShopTabType.Topping:
                    RefreshToppingTab();
                    break;
                case ShopTabType.Upgrade:
                    RefreshUpgradeTab();
                    break;
                case ShopTabType.Decoration:
                    RefreshDecorationTab();
                    break;
            }
        }

        private void RefreshToppingTab()
        {
            // 기본 해금 재료는 상점에 노출하지 않는다.
            List<IngredientData> lockedByDefault = new List<IngredientData>();

            if (allIngredients != null)
            {
                for (int i = 0; i < allIngredients.Length; i++)
                {
                    IngredientData data = allIngredients[i];
                    if (data != null && !data.isDefaultUnlocked)
                    {
                        lockedByDefault.Add(data);
                    }
                }
            }

            GameState state = State;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= lockedByDefault.Count)
                {
                    // TODO(기획확인): 남는 칸 처리 방식이 스토리보드에 없어 우선 숨긴다.
                    slots[i].gameObject.SetActive(false);
                    continue;
                }

                IngredientData data = lockedByDefault[i];
                bool isPurchased = state != null && state.IsIngredientUnlocked(data.id);

                slots[i].gameObject.SetActive(true);
                slots[i].Setup(ShopTabType.Topping, data.id, data.icon, data.ingredientName,
                               data.unlockCost, isPurchased, true, OnSlotClicked);
            }

            AdjustSlotEndPadding(lockedByDefault.Count);
        }

        private void RefreshUpgradeTab()
        {
            GameState state = State;
            int upgradeCount = allUpgrades != null ? allUpgrades.Length : 0;

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].gameObject.SetActive(true);

                if (i >= upgradeCount || allUpgrades[i] == null)
                {
                    // 배열보다 슬롯이 많으면 남는 칸은 "공사중"으로 잠근다.
                    slots[i].Setup(ShopTabType.Upgrade, -1, null, underConstructionLabel,
                                   0, false, false, null);
                    continue;
                }

                UpgradeData upgrade = allUpgrades[i];
                int level = state != null ? state.GetUpgradeLevel(upgrade.id) : 0;
                bool isMaxLevel = level >= upgrade.maxLevel;

                string label = string.Format(upgradeLabelFormat, upgrade.name, level, upgrade.maxLevel);

                // TODO(기획확인): 최대 레벨에서 비용 칸에 무엇을 띄울지 미확정. 현재는 0(=0.0C)이 나온다.
                int cost = 0;
                if (!isMaxLevel && upgrade.costPerLevel != null && level < upgrade.costPerLevel.Count)
                {
                    cost = upgrade.costPerLevel[level];
                }

                slots[i].Setup(ShopTabType.Upgrade, upgrade.id, upgrade.icon, label,
                               cost, isMaxLevel, true, OnSlotClicked);
            }

            AdjustSlotEndPadding(upgradeCount);
        }

        private void RefreshDecorationTab()
        {
            GameState state = State;
            int decorationCount = allDecorations != null ? allDecorations.Length : 0;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= decorationCount || allDecorations[i] == null)
                {
                    // TODO(기획확인): 남는 칸 처리 방식이 스토리보드에 없어 우선 숨긴다.
                    slots[i].gameObject.SetActive(false);
                    continue;
                }

                DecorationData data = allDecorations[i];
                bool isPurchased = state != null && state.IsDecorationPurchased(data.id);

                slots[i].gameObject.SetActive(true);
                slots[i].Setup(ShopTabType.Decoration, data.id, data.sprite, data.decorationName,
                               data.cost, isPurchased, true, OnSlotClicked);
            }

            AdjustSlotEndPadding(decorationCount);
        }

        private void OnSlotClicked(ShopSlotUI slot)
        {
            GameState state = State;
            if (state == null)
            {
                ShowMessage(noGameManagerMessage);
                return;
            }

            switch (slot.Tab)
            {
                case ShopTabType.Topping:
                    TryPurchaseIngredient(state, slot);
                    break;
                case ShopTabType.Upgrade:
                    TryPurchaseUpgrade(state, slot);
                    break;
                case ShopTabType.Decoration:
                    TryPurchaseDecoration(state, slot);
                    break;
            }

            RefreshHud();
        }

        private void TryPurchaseIngredient(GameState state, ShopSlotUI slot)
        {
            IngredientData data = FindIngredient(slot.TargetId);
            if (data == null || state.IsIngredientUnlocked(data.id))
            {
                return;
            }

            if (!TrySpend(state, data.unlockCost))
            {
                return;
            }

            state.UnlockIngredient(data.id);
            slot.MarkPurchased();
            ClearMessage();
            SaveProgress();
        }

        private void TryPurchaseUpgrade(GameState state, ShopSlotUI slot)
        {
            UpgradeData upgrade = FindUpgrade(slot.TargetId);
            if (upgrade == null)
            {
                return;
            }

            int level = state.GetUpgradeLevel(upgrade.id);
            if (level >= upgrade.maxLevel)
            {
                return;
            }

            if (upgrade.costPerLevel == null || level >= upgrade.costPerLevel.Count)
            {
                // TODO(기획확인): 비용 표가 maxLevel보다 짧을 때의 처리 미확정.
                return;
            }

            if (!TrySpend(state, upgrade.costPerLevel[level]))
            {
                return;
            }

            state.SetUpgradeLevel(upgrade.id, level + 1);
            ClearMessage();
            SaveProgress();

            // 레벨 라벨과 다음 비용이 함께 바뀌므로 그리드를 다시 그린다.
            RefreshUpgradeTab();
        }

        private void TryPurchaseDecoration(GameState state, ShopSlotUI slot)
        {
            DecorationData data = FindDecoration(slot.TargetId);
            if (data == null || state.IsDecorationPurchased(data.id))
            {
                return;
            }

            if (!TrySpend(state, data.cost))
            {
                return;
            }

            state.PurchaseDecoration(data.id);
            slot.MarkPurchased();
            ClearMessage();
            SaveProgress();
            CounterDecorationPlacementSession.Begin(data);
            SceneTransitionManager.LoadSceneSlideLeft(counterSceneName);
        }

        private void AdjustSlotEndPadding(int visibleSlotCount)
        {
            if (slotScrollRect == null || slotScrollRect.content == null)
            {
                return;
            }

            GridLayoutGroup grid = slotScrollRect.content.GetComponent<GridLayoutGroup>();
            RectTransform viewport = slotScrollRect.viewport;
            if (grid == null || viewport == null || visibleSlotCount <= 0)
            {
                return;
            }

            float centerPadding = Mathf.Max(0f, (viewport.rect.width - grid.cellSize.x) * 0.5f);
            int roundedPadding = Mathf.RoundToInt(centerPadding);
            grid.padding.left = Mathf.Max(grid.padding.left, roundedPadding);
            grid.padding.right = Mathf.Max(grid.padding.right, roundedPadding);

            LayoutRebuilder.ForceRebuildLayoutImmediate(slotScrollRect.content);
        }

        private bool TrySpend(GameState state, int cost)
        {
            if (state.gold < cost)
            {
                ShowMessage(insufficientGoldMessage);
                return false;
            }

            state.gold -= cost;
            return true;
        }

        private void OnRepayConfirm()
        {
            GameState state = State;
            if (state == null)
            {
                ShowMessage(noGameManagerMessage);
                return;
            }

            if (state.debtRemaining <= 0)
            {
                ShowMessage(debtClearedMessage);
                return;
            }

            string raw = repayInputField != null ? repayInputField.text : string.Empty;

            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float actualAmount) || actualAmount <= 0f)
            {
                ShowMessage(invalidAmountMessage);
                return;
            }

            int storedAmount = CurrencyUtil.ToStored(actualAmount);
            if (storedAmount <= 0)
            {
                // 0.04 같은 값이 0으로 반올림되는 경우.
                ShowMessage(invalidAmountMessage);
                return;
            }

            if (storedAmount > state.gold)
            {
                ShowMessage(notEnoughGoldForRepayMessage);
                return;
            }

            // 남은 빚보다 많이 내도 초과분은 차감하지 않는다.
            int payment = Mathf.Min(storedAmount, state.debtRemaining);

            state.gold -= payment;
            state.debtRemaining -= payment;
            SaveProgress();

            if (repayInputField != null)
            {
                repayInputField.text = string.Empty;
            }

            if (state.debtRemaining <= 0)
            {
                ShowMessage(debtClearedMessage);
            }
            else
            {
                ClearMessage();
            }

            RefreshDebtPanel();
            RefreshHud();
        }

        private static void SaveProgress()
        {
            SheepSheepBurger.Core.GameManager.SaveCurrentGame();
        }

        private void RefreshDebtPanel()
        {
            GameState state = State;

            if (debtRemainingText != null)
            {
                // TODO(기획확인): "잔여 부채" 같은 라벨 문구가 스토리보드에 없어 금액만 표시한다.
                debtRemainingText.text = state != null ? CurrencyUtil.ToDisplay(state.debtRemaining) : string.Empty;
            }
        }

        private void RefreshHud()
        {
            GameState state = State;
            if (state == null)
            {
                return;
            }

            if (goldText != null)
            {
                goldText.text = CurrencyUtil.ToDisplay(state.gold);
            }

            if (dDayText != null)
            {
                dDayText.text = string.Format(dDayFormat, state.debtDeadline - state.currentDay);
            }
        }

        private IngredientData FindIngredient(int id)
        {
            if (allIngredients == null)
            {
                return null;
            }

            for (int i = 0; i < allIngredients.Length; i++)
            {
                if (allIngredients[i] != null && allIngredients[i].id == id)
                {
                    return allIngredients[i];
                }
            }

            return null;
        }

        private UpgradeData FindUpgrade(int id)
        {
            if (allUpgrades == null)
            {
                return null;
            }

            for (int i = 0; i < allUpgrades.Length; i++)
            {
                if (allUpgrades[i] != null && allUpgrades[i].id == id)
                {
                    return allUpgrades[i];
                }
            }

            return null;
        }

        private DecorationData FindDecoration(int id)
        {
            if (allDecorations == null)
            {
                return null;
            }

            for (int i = 0; i < allDecorations.Length; i++)
            {
                if (allDecorations[i] != null && allDecorations[i].id == id)
                {
                    return allDecorations[i];
                }
            }

            return null;
        }

        private void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void ClearMessage()
        {
            ShowMessage(string.Empty);
        }
    }
}
