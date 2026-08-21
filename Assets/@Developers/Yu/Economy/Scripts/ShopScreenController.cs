using System.Collections.Generic;
using System.Globalization;
using Core.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SheepSheepBurger.Economy
{
    [DisallowMultipleComponent]
    public sealed class ShopScreenController : MonoBehaviour, IScrollHandler
    {
        private const float ReferenceWidth = 1600f;
        private const float ReferenceHeight = 900f;
        private const string ShopBackgroundResourcePath = "Shop/ShopBackgroundFull";
        private const string ShopCardResourcePath = "Shop/ShopItemCard";
        private const string ShopCategoryButtonResourcePath = "Shop/ShopCategoryButton";
        private const string ShopCardButtonResourcePath = "Shop/ShopCardButton";
        private static readonly Rect ShopCardSpriteRect = new Rect(401f, 125f, 252f, 366f);
        private static readonly Rect ShopCategoryButtonSpriteRect = new Rect(36f, 932f, 365f, 150f);
        private static readonly Rect ShopCardButtonSpriteRect = new Rect(0f, 0f, 320f, 137f);

        private readonly Dictionary<ShopCategory, Button> categoryButtons = new Dictionary<ShopCategory, Button>();
        private readonly List<GameObject> itemCards = new List<GameObject>();

        [SerializeField] private bool loadFromPlayerPrefs = true;
        [SerializeField] private float debugStartingMoney = 0f;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private ShopCategory selectedCategory = ShopCategory.Topping;
        [SerializeField] private int selectedItemIndex;

        private ShopCatalog catalog;
        private EconomyService economy;
        private PlayerEconomyState state;
        private RectTransform canvasRoot;
        private RectTransform cardRoot;
        private RectTransform cardContent;
        private Text moneyText;
        private Text statusText;
        private InputField debtInput;
        private Font uiFont;

        public PlayerEconomyState State => state;

        private void Awake()
        {
            catalog = ShopCatalog.CreateFromGameDatabase(gameDatabase);
            economy = new EconomyService(catalog);
            state = loadFromPlayerPrefs
                ? PlayerPrefsEconomyStore.LoadOrDefault()
                : PlayerEconomyState.CreateNewGame(debugStartingMoney);

            BuildInterface();
            Refresh();
        }

        public void SelectCategory(ShopCategory category)
        {
            selectedCategory = category;
            selectedItemIndex = 0;
            Refresh();
        }

        public void MoveSelection(int direction)
        {
            List<ShopItemData> items = catalog.GetItems(selectedCategory);
            if (items.Count <= 3)
            {
                return;
            }

            int pageCount = Mathf.CeilToInt(items.Count / 3f);
            int currentPage = Mathf.Clamp(selectedItemIndex / 3, 0, pageCount - 1);
            int nextPage = (currentPage + direction + pageCount) % pageCount;
            selectedItemIndex = nextPage * 3;
            Refresh();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (selectedCategory == ShopCategory.Repair || selectedCategory == ShopCategory.Debt)
            {
                return;
            }

            float delta = Mathf.Abs(eventData.scrollDelta.y) >= Mathf.Abs(eventData.scrollDelta.x)
                ? eventData.scrollDelta.y
                : -eventData.scrollDelta.x;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return;
            }

            MoveSelection(delta < 0f ? 1 : -1);
            eventData.Use();
        }

        public void GiveDebugMoney(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            state.money = EconomyRules.RoundMoney(state.money + amount);
            Save();
            Refresh();
        }

        private void TryBuy(ShopItemId itemId)
        {
            if (itemId == ShopItemId.FryerUpgrade)
            {
                bool upgraded = economy.TryBuyToolUpgrade(state, ToolUpgradeType.Fryer, out ToolUpgradeData data);
                statusText.text = upgraded ? "Fryer upgraded to Lv." + data.level : "Cannot upgrade fryer.";
            }
            else if (itemId == ShopItemId.GrillPlateUpgrade)
            {
                bool upgraded = economy.TryBuyToolUpgrade(state, ToolUpgradeType.GrillPlate, out ToolUpgradeData data);
                statusText.text = upgraded ? "Grill upgraded to Lv." + data.level : "Cannot upgrade grill.";
            }
            else if (itemId == ShopItemId.MedicalCare)
            {
                MedicalBillEvent bill = economy.RecordMedicalBill(state);
                statusText.text = bill.description + " +" + FormatMoney(bill.cost);
            }
            else
            {
                ShopPurchaseResult result = economy.TryBuyItem(state, itemId);
                statusText.text = result.message;
            }

            Save();
            Refresh();
        }

        private void RecordRepairDamage(RepairDamageSeverity severity)
        {
            RepairDamageEvent damageEvent = economy.RecordRepairDamage(state, severity);
            statusText.text = damageEvent.description + " +" + FormatMoney(damageEvent.cost);
            Save();
            Refresh();
        }

        private void TryPayDebtFromInput()
        {
            if (debtInput == null)
            {
                return;
            }

            if (!TryParseMoney(debtInput.text, out float requestedAmount) || requestedAmount <= 0f)
            {
                statusText.text = "Enter a debt payment amount.";
                return;
            }

            bool paidAny = economy.TryPayDebt(state, requestedAmount, out float paid);
            statusText.text = paidAny
                ? "Debt paid: " + FormatMoney(paid)
                : "Cannot pay debt right now.";
            Save();
            Refresh();
        }

        private void FillDebtInputWithMax()
        {
            if (debtInput == null)
            {
                return;
            }

            float maxPayment = EconomyRules.RoundMoney(Mathf.Min(state.money, state.debtRemaining));
            debtInput.text = maxPayment.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private void Save()
        {
            if (loadFromPlayerPrefs)
            {
                PlayerPrefsEconomyStore.Save(state);
            }
        }

        private void BuildInterface()
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            GameObject canvasObject = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            SetStretch(canvasRoot);

            RectTransform background = CreateImage("ShopBackground", canvasRoot, Hex("#F3D9A7"), Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), false);
            SetStretch(background);
            bool hasShopArt = TryApplyShopBackground(background.GetComponent<Image>());

            RectTransform categoryParent = canvasRoot;
            if (!hasShopArt)
            {
                CreateImage("ShopPanel", canvasRoot, Hex("#42D459"), new Vector2(135f, -20f), new Vector2(1430f, 620f), false);
                categoryParent = CreateImage("CategoryPanel", canvasRoot, Hex("#DDF4D5"), new Vector2(-665f, -20f), new Vector2(280f, 640f), false);
            }

            BuildCategoryButton(categoryParent, ShopCategory.Topping, "Toppings", hasShopArt ? new Vector2(-630f, 245f) : new Vector2(0f, 220f));
            BuildCategoryButton(categoryParent, ShopCategory.Upgrade, "Upgrades", hasShopArt ? new Vector2(-630f, 135f) : new Vector2(0f, 110f));
            BuildCategoryButton(categoryParent, ShopCategory.Repair, "Repair", hasShopArt ? new Vector2(-630f, 25f) : new Vector2(0f, 0f));
            BuildCategoryButton(categoryParent, ShopCategory.Debt, "Debt", hasShopArt ? new Vector2(-630f, -85f) : new Vector2(0f, -110f));
            BuildCategoryButton(categoryParent, ShopCategory.Decoration, "Decor", hasShopArt ? new Vector2(-630f, -195f) : new Vector2(0f, -220f));

            moneyText = CreateText("MoneyText", canvasRoot, string.Empty, 26, FontStyle.Bold, Hex("#1F3B22"), hasShopArt ? new Vector2(345f, 305f) : new Vector2(330f, 350f), new Vector2(900f, 48f));
            statusText = CreateText("ShopStatus", canvasRoot, string.Empty, 22, FontStyle.Bold, Hex("#1F3B22"), hasShopArt ? new Vector2(390f, -330f) : new Vector2(330f, -360f), new Vector2(950f, 48f));

            GameObject cardRootObject = new GameObject("ShopCardRoot", typeof(RectTransform));
            cardRoot = cardRootObject.GetComponent<RectTransform>();
            cardRoot.SetParent(canvasRoot, false);
            SetRect(cardRoot, hasShopArt ? new Vector2(175f, -35f) : new Vector2(135f, -20f), hasShopArt ? new Vector2(1240f, 720f) : new Vector2(1260f, 720f));

            Image viewportImage = cardRootObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
            Mask mask = cardRootObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            GameObject contentObject = new GameObject("ShopCardContent", typeof(RectTransform));
            cardContent = contentObject.GetComponent<RectTransform>();
            cardContent.SetParent(cardRoot, false);
            cardContent.anchorMin = new Vector2(0.5f, 1f);
            cardContent.anchorMax = new Vector2(0.5f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = cardRoot.sizeDelta;
            CreateEventSystem();
        }

        private void BuildCategoryButton(RectTransform parent, ShopCategory category, string label, Vector2 position)
        {
            RectTransform rect = CreateImage(category + "Button", parent, Hex("#81DB8F"), position, new Vector2(225f, 92f), true);
            TryApplyShopCategoryButton(rect.GetComponent<Image>());
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            ShopCategory targetCategory = category;
            button.onClick.AddListener(() => SelectCategory(targetCategory));
            CreateText(category + "Label", rect, label, 23, FontStyle.Bold, Hex("#3B2A18"), Vector2.zero, new Vector2(185f, 56f));
            categoryButtons[category] = button;
        }

        private void Refresh()
        {
            state.Sanitize();
            moneyText.text = "Money " + FormatMoney(state.money) + "   Debt " + FormatMoney(state.debtRemaining) + "/" + FormatMoney(EconomyRules.StartingDebt) + "   Day " + state.dayNumber;
            if (string.IsNullOrEmpty(statusText.text))
            {
                statusText.text = "Unlocked ingredients stay unlocked. Ingredient cost is charged only when used.";
            }

            foreach (KeyValuePair<ShopCategory, Button> pair in categoryButtons)
            {
                Image image = pair.Value.GetComponent<Image>();
                image.color = pair.Key == selectedCategory ? Hex("#FFF1C8") : Color.white;
            }

            RebuildCards();
        }

        private void RebuildCards()
        {
            for (int index = 0; index < itemCards.Count; index++)
            {
                Destroy(itemCards[index]);
            }
            itemCards.Clear();
            debtInput = null;
            ResetCardContent(cardRoot.sizeDelta.y);

            if (selectedCategory == ShopCategory.Repair)
            {
                CreateRepairPanel();
                return;
            }

            if (selectedCategory == ShopCategory.Debt)
            {
                CreateDebtPanel();
                return;
            }

            List<ShopItemData> items = catalog.GetItems(selectedCategory);
            if (items.Count == 0)
            {
                Text empty = CreateText("EmptyCategory", cardContent, "Coming soon", 30, FontStyle.Bold, Hex("#1F3B22"), Vector2.zero, new Vector2(330f, 80f));
                itemCards.Add(empty.gameObject);
                return;
            }

            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, Mathf.Max(0, items.Count - 1));
            int pageStartIndex = (selectedItemIndex / 3) * 3;
            CreateItemGrid(items, pageStartIndex);
            if (items.Count > 3)
            {
                CreatePagerButton("PreviousPage", "<", new Vector2(-600f, 0f), -1);
                CreatePagerButton("NextPage", ">", new Vector2(600f, 0f), 1);
            }
        }

        private void CreateRepairPanel()
        {
            ResetCardContent(cardRoot.sizeDelta.y);
            RectTransform panel = CreateImage("RepairPanel", cardContent, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, new Vector2(476f, 686f), false);
            TryApplyShopCardFrame(panel.GetComponent<Image>());
            itemCards.Add(panel.gameObject);
            CreateText("RepairTitle", panel, "Repair / Medical", 28, FontStyle.Bold, Color.black, new Vector2(0f, 238f), new Vector2(410f, 54f));
            CreateText(
                "RepairSummary",
                panel,
                "Repair: " + state.repairIncidentsToday + " incidents / " + FormatMoney(state.repairCostToday) +
                "\nMedical: " + state.medicalIncidentsToday + " incidents / " + FormatMoney(state.medicalCostToday) +
                "\nCharged at day close.",
                20,
                FontStyle.Bold,
                Color.black,
                new Vector2(0f, 132f),
                new Vector2(410f, 112f));

            CreateRepairButton(panel, RepairDamageSeverity.Minor, new Vector2(-118f, -70f));
            CreateRepairButton(panel, RepairDamageSeverity.Moderate, new Vector2(118f, -70f));
            CreateRepairButton(panel, RepairDamageSeverity.Major, new Vector2(-118f, -170f));
            CreateRepairButton(panel, RepairDamageSeverity.Severe, new Vector2(118f, -170f));
            CreateMedicalButton(panel, new Vector2(0f, -272f));
        }

        private void CreateRepairButton(RectTransform parent, RepairDamageSeverity severity, Vector2 position)
        {
            float cost = EconomyRules.GetRepairCost(severity);
            RectTransform rect = CreateImage(severity + "RepairButton", parent, Hex("#F08A5C"), position, new Vector2(140f, 74f), true);
            TryApplyShopCardButton(rect.GetComponent<Image>());
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            RepairDamageSeverity targetSeverity = severity;
            button.onClick.AddListener(() => RecordRepairDamage(targetSeverity));
            CreateText(severity + "RepairLabel", rect, severity + "\n" + FormatMoney(cost), 17, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateMedicalButton(RectTransform parent, Vector2 position)
        {
            RectTransform rect = CreateImage("MedicalCareButton", parent, Hex("#F08A5C"), position, new Vector2(180f, 58f), true);
            TryApplyShopCardButton(rect.GetComponent<Image>());
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => TryBuy(ShopItemId.MedicalCare));
            CreateText("MedicalCareLabel", rect, "Medical\n" + FormatMoney(EconomyRules.MedicalCareCost), 17, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateDebtPanel()
        {
            ResetCardContent(cardRoot.sizeDelta.y);
            RectTransform panel = CreateImage("DebtPanel", cardContent, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, new Vector2(476f, 686f), false);
            TryApplyShopCardFrame(panel.GetComponent<Image>());
            itemCards.Add(panel.gameObject);

            int daysLeft = Mathf.Max(0, EconomyRules.DebtDeadlineDays - state.dayNumber + 1);
            CreateText("DebtTitle", panel, "Debt Repayment", 28, FontStyle.Bold, Color.black, new Vector2(0f, 238f), new Vector2(410f, 54f));
            CreateText(
                "DebtSummary",
                panel,
                "Remaining: " + FormatMoney(state.debtRemaining) +
                "\nMoney: " + FormatMoney(state.money) +
                "\nDays left: " + daysLeft,
                21,
                FontStyle.Bold,
                Color.black,
                new Vector2(0f, 138f),
                new Vector2(410f, 112f));

            debtInput = CreateInputField("DebtAmountInput", panel, "Amount", new Vector2(0f, -40f), new Vector2(230f, 58f));
            FillDebtInputWithMax();

            CreateDebtActionButton(panel, "PayDebtButton", "Pay Debt", new Vector2(0f, -160f), new Vector2(220f, 62f), TryPayDebtFromInput);
            CreateDebtActionButton(panel, "MaxDebtButton", "Max", new Vector2(0f, -270f), new Vector2(150f, 50f), FillDebtInputWithMax);
        }

        private void CreateDebtActionButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateImage(name, parent, Hex("#F08A5C"), position, size, true);
            TryApplyShopCardButton(rect.GetComponent<Image>());
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            CreateText(name + "Label", rect, label, 20, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateItemCard(ShopItemData item, Vector2 position, int itemNumber, int itemCount)
        {
            CreateItemCard(item, position, itemNumber, itemCount, new Vector2(238f, 342f), true);
        }

        private void CreateItemGrid(List<ShopItemData> items, int pageStartIndex)
        {
            int visibleCount = Mathf.Min(3, items.Count - pageStartIndex);
            int columns = Mathf.Min(3, visibleCount);
            int rows = 1;
            float cardWidth = 378f;
            float cardHeight = 543f;
            float horizontalGap = 20f;
            float verticalGap = 0f;
            float totalWidth = (columns * cardWidth) + ((columns - 1) * horizontalGap);
            float totalHeight = (rows * cardHeight) + ((rows - 1) * verticalGap);
            float viewportHeight = cardRoot.sizeDelta.y;
            float contentHeight = viewportHeight;
            float topPadding = (contentHeight - totalHeight) * 0.5f;
            Vector2 cardSize = new Vector2(cardWidth, cardHeight);
            ResetCardContent(contentHeight);

            for (int index = 0; index < visibleCount; index++)
            {
                int column = index % columns;
                int row = index / columns;
                float x = (-totalWidth * 0.5f) + (cardWidth * 0.5f) + (column * (cardWidth + horizontalGap));
                float y = (contentHeight * 0.5f) - topPadding - (cardHeight * 0.5f) - (row * (cardHeight + verticalGap));
                int itemIndex = pageStartIndex + index;
                CreateItemCard(items[itemIndex], new Vector2(x, y), itemIndex + 1, items.Count, cardSize, false);
            }
        }

        private void CreateItemCard(ShopItemData item, Vector2 position, int itemNumber, int itemCount, Vector2 size, bool showCounter)
        {
            RectTransform card = CreateImage(item.id + "Card", cardContent, new Color(1f, 1f, 1f, 0.9f), position, size, false);
            bool hasCardFrame = TryApplyShopCardFrame(card.GetComponent<Image>());
            itemCards.Add(card.gameObject);
            float scale = 1f;
            if (showCounter)
            {
                CreateText(item.id + "Counter", card, itemNumber + " / " + itemCount, Mathf.RoundToInt(18 * scale), FontStyle.Bold, Hex("#637064"), new Vector2(0f, 205f * scale), new Vector2(300f * scale, 34f * scale));
            }

            CreateText(item.id + "Name", card, item.displayName, Mathf.RoundToInt((hasCardFrame ? 26 : 30) * scale), FontStyle.Bold, Color.black, hasCardFrame ? new Vector2(0f, 30f * scale) : new Vector2(0f, 135f * scale), new Vector2(260f * scale, 62f * scale));
            CreateText(item.id + "Flavor", card, item.flavorText, Mathf.RoundToInt(19 * scale), FontStyle.Bold, Hex("#3B2A18"), hasCardFrame ? new Vector2(0f, -52f * scale) : new Vector2(0f, 45f * scale), new Vector2(260f * scale, 88f * scale));

            float displayPrice = GetDisplayPrice(item);
            bool complete = IsItemComplete(item);
            bool blockedByDebt = item.category == ShopCategory.Decoration && state.debtRemaining > 0f;
            bool canAfford = state.money >= displayPrice && !complete && !blockedByDebt;
            string buttonText = GetPurchaseButtonText(item, complete, blockedByDebt, displayPrice);
            Color buttonColor = complete ? Hex("#C9D2CA") : (canAfford ? Hex("#F08A5C") : Hex("#E5E5E5"));
            RectTransform buyRect = CreateImage(item.id + "Buy", card, buttonColor, new Vector2(0f, -165f * scale), new Vector2(220f * scale, 94f * scale), true);
            TryApplyShopCardButton(buyRect.GetComponent<Image>());
            Button buyButton = buyRect.gameObject.AddComponent<Button>();
            buyButton.targetGraphic = buyRect.GetComponent<Image>();
            buyButton.interactable = canAfford;
            ShopItemId targetItem = item.id;
            buyButton.onClick.AddListener(() => TryBuy(targetItem));
            CreateText(item.id + "BuyLabel", buyRect, buttonText, Mathf.RoundToInt(21 * scale), FontStyle.Bold, Color.black, Vector2.zero, buyRect.sizeDelta);
        }

        private void CreatePagerButton(string name, string label, Vector2 position, int direction)
        {
            RectTransform rect = CreateImage(name, cardRoot, new Color(1f, 1f, 1f, 0f), position, new Vector2(78f, 90f), true);
            itemCards.Add(rect.gameObject);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => MoveSelection(direction));
            CreateText(name + "Label", rect, label, 44, FontStyle.Bold, Hex("#3B2A18"), Vector2.zero, rect.sizeDelta);
        }

        private float GetDisplayPrice(ShopItemData item)
        {
            if (item.id == ShopItemId.FryerUpgrade)
            {
                return EconomyRules.GetToolUpgradeCost(ToolUpgradeType.Fryer, state.fryerUpgradeLevel);
            }

            if (item.id == ShopItemId.GrillPlateUpgrade)
            {
                return EconomyRules.GetToolUpgradeCost(ToolUpgradeType.GrillPlate, state.grillPlateUpgradeLevel);
            }

            return item.price;
        }

        private bool IsItemComplete(ShopItemData item)
        {
            if (item.unlocksIngredient && state.HasPurchased(item.id))
            {
                return true;
            }

            if (item.id == ShopItemId.FryerUpgrade)
            {
                return state.fryerUpgradeLevel >= EconomyRules.MaxToolUpgradeLevel;
            }

            if (item.id == ShopItemId.GrillPlateUpgrade)
            {
                return state.grillPlateUpgradeLevel >= EconomyRules.MaxToolUpgradeLevel;
            }

            return false;
        }

        private string GetPurchaseButtonText(ShopItemData item, bool complete, bool blockedByDebt, float displayPrice)
        {
            if (complete)
            {
                return item.unlocksIngredient ? "Unlocked" : "Max Lv.";
            }

            if (blockedByDebt)
            {
                return "Debt Locked";
            }

            if (item.id == ShopItemId.FryerUpgrade)
            {
                return "Lv." + state.fryerUpgradeLevel + " > " + FormatMoney(displayPrice);
            }

            if (item.id == ShopItemId.GrillPlateUpgrade)
            {
                return "Lv." + state.grillPlateUpgradeLevel + " > " + FormatMoney(displayPrice);
            }

            return FormatMoney(displayPrice);
        }

        private static bool TryApplyShopBackground(Image image)
        {
            return TryApplyResourceSprite(image, ShopBackgroundResourcePath, new Rect(), false);
        }

        private static bool TryApplyShopCardFrame(Image image)
        {
            return TryApplyResourceSprite(image, ShopCardResourcePath, ShopCardSpriteRect, true);
        }

        private static bool TryApplyShopCategoryButton(Image image)
        {
            return TryApplyResourceSprite(image, ShopCategoryButtonResourcePath, ShopCategoryButtonSpriteRect, true);
        }

        private static bool TryApplyShopCardButton(Image image)
        {
            return TryApplyResourceSprite(image, ShopCardButtonResourcePath, ShopCardButtonSpriteRect, true);
        }

        private static bool TryApplyResourceSprite(Image image, string resourcePath, Rect sourceRect, bool useSourceRect)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return false;
            }

            Rect spriteRect = useSourceRect ? sourceRect : new Rect(0f, 0f, texture.width, texture.height);
            image.sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f);
            image.color = Color.white;
            image.preserveAspect = false;
            return true;
        }

        private static string FormatMoney(float value)
        {
            return EconomyRules.RoundMoney(value).ToString("0.#") + "C";
        }

        private static bool TryParseMoney(string value, out float amount)
        {
            amount = 0f;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim().Replace("C", string.Empty).Replace("c", string.Empty);
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out amount) ||
                   float.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out amount);
        }

        private RectTransform CreateImage(string name, RectTransform parent, Color color, Vector2 position, Vector2 size, bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, dimensions);
            Text text = gameObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 8);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private InputField CreateInputField(string name, RectTransform parent, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);

            Image image = gameObject.GetComponent<Image>();
            image.color = Hex("#FFF5E8");
            image.raycastTarget = true;

            Text inputText = CreateInputChildText("Text", rect, string.Empty, Color.black, FontStyle.Bold);
            Text placeholderText = CreateInputChildText("Placeholder", rect, placeholder, Hex("#8F8F8F"), FontStyle.Normal);

            InputField input = gameObject.GetComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.contentType = InputField.ContentType.DecimalNumber;
            input.lineType = InputField.LineType.SingleLine;
            input.caretColor = Color.black;
            input.selectionColor = new Color(0.95f, 0.54f, 0.36f, 0.45f);
            input.targetGraphic = image;
            return input;
        }

        private Text CreateInputChildText(string name, RectTransform parent, string value, Color color, FontStyle style)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(14f, 4f);
            rect.offsetMax = new Vector2(-14f, -4f);

            Text text = gameObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = 22;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ResetCardContent(float height)
        {
            if (cardContent == null)
            {
                return;
            }

            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = new Vector2(cardRoot.sizeDelta.x, Mathf.Max(cardRoot.sizeDelta.y, height));
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.magenta;
        }

        private static void CreateEventSystem()
        {
            EventSystem existing = FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
