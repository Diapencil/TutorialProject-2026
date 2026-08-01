using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SheepSheepBurger.Economy
{
    [DisallowMultipleComponent]
    public sealed class ShopScreenController : MonoBehaviour
    {
        private const float ReferenceWidth = 1600f;
        private const float ReferenceHeight = 900f;
        private const string ShopBackgroundResourcePath = "Shop/ShopBackground";

        private readonly Dictionary<ShopCategory, Button> categoryButtons = new Dictionary<ShopCategory, Button>();
        private readonly List<GameObject> itemCards = new List<GameObject>();

        [SerializeField] private bool loadFromPlayerPrefs = true;
        [SerializeField] private float debugStartingMoney = 0f;
        [SerializeField] private ShopCategory selectedCategory = ShopCategory.Topping;
        [SerializeField] private int selectedItemIndex;

        private ShopCatalog catalog;
        private EconomyService economy;
        private PlayerEconomyState state;
        private RectTransform canvasRoot;
        private RectTransform cardRoot;
        private Text moneyText;
        private Text statusText;
        private InputField debtInput;
        private Font uiFont;

        public PlayerEconomyState State => state;

        private void Awake()
        {
            catalog = ShopCatalog.CreateDefault();
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
            if (items.Count <= 1)
            {
                return;
            }

            selectedItemIndex = (selectedItemIndex + direction + items.Count) % items.Count;
            Refresh();
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
            SetRect(cardRoot, hasShopArt ? new Vector2(-80f, -40f) : new Vector2(135f, -20f), hasShopArt ? new Vector2(380f, 500f) : new Vector2(1260f, 560f));

            CreateEventSystem();
        }

        private void BuildCategoryButton(RectTransform parent, ShopCategory category, string label, Vector2 position)
        {
            RectTransform rect = CreateImage(category + "Button", parent, Hex("#81DB8F"), position, new Vector2(220f, 80f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            ShopCategory targetCategory = category;
            button.onClick.AddListener(() => SelectCategory(targetCategory));
            CreateText(category + "Label", rect, label, 24, FontStyle.Bold, Hex("#173820"), Vector2.zero, rect.sizeDelta);
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
                image.color = pair.Key == selectedCategory ? Hex("#BFF0C5") : Hex("#81DB8F");
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
                Text empty = CreateText("EmptyCategory", cardRoot, "Coming soon", 30, FontStyle.Bold, Hex("#1F3B22"), Vector2.zero, new Vector2(330f, 80f));
                itemCards.Add(empty.gameObject);
                return;
            }

            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, items.Count - 1);
            CreateItemCard(items[selectedItemIndex], Vector2.zero, selectedItemIndex + 1, items.Count);
            if (items.Count > 1)
            {
                CreatePagerButton("PreviousItem", "<", new Vector2(-270f, -10f), -1);
                CreatePagerButton("NextItem", ">", new Vector2(270f, -10f), 1);
            }
        }

        private void CreateRepairPanel()
        {
            RectTransform panel = CreateImage("RepairPanel", cardRoot, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, new Vector2(340f, 470f), false);
            itemCards.Add(panel.gameObject);
            CreateText("RepairTitle", panel, "Repair / Medical", 28, FontStyle.Bold, Color.black, new Vector2(0f, 170f), new Vector2(310f, 54f));
            CreateText(
                "RepairSummary",
                panel,
                "Repair: " + state.repairIncidentsToday + " incidents / " + FormatMoney(state.repairCostToday) +
                "\nMedical: " + state.medicalIncidentsToday + " incidents / " + FormatMoney(state.medicalCostToday) +
                "\nCharged at day close.",
                20,
                FontStyle.Bold,
                Color.black,
                new Vector2(0f, 65f),
                new Vector2(310f, 130f));

            CreateRepairButton(panel, RepairDamageSeverity.Minor, new Vector2(-80f, -55f));
            CreateRepairButton(panel, RepairDamageSeverity.Moderate, new Vector2(80f, -55f));
            CreateRepairButton(panel, RepairDamageSeverity.Major, new Vector2(-80f, -135f));
            CreateRepairButton(panel, RepairDamageSeverity.Severe, new Vector2(80f, -135f));
            CreateMedicalButton(panel, new Vector2(0f, -210f));
        }

        private void CreateRepairButton(RectTransform parent, RepairDamageSeverity severity, Vector2 position)
        {
            float cost = EconomyRules.GetRepairCost(severity);
            RectTransform rect = CreateImage(severity + "RepairButton", parent, Hex("#F08A5C"), position, new Vector2(140f, 74f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            RepairDamageSeverity targetSeverity = severity;
            button.onClick.AddListener(() => RecordRepairDamage(targetSeverity));
            CreateText(severity + "RepairLabel", rect, severity + "\n" + FormatMoney(cost), 17, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateMedicalButton(RectTransform parent, Vector2 position)
        {
            RectTransform rect = CreateImage("MedicalCareButton", parent, Hex("#F08A5C"), position, new Vector2(180f, 58f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => TryBuy(ShopItemId.MedicalCare));
            CreateText("MedicalCareLabel", rect, "Medical\n" + FormatMoney(EconomyRules.MedicalCareCost), 17, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateDebtPanel()
        {
            RectTransform panel = CreateImage("DebtPanel", cardRoot, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, new Vector2(340f, 470f), false);
            itemCards.Add(panel.gameObject);

            int daysLeft = Mathf.Max(0, EconomyRules.DebtDeadlineDays - state.dayNumber + 1);
            CreateText("DebtTitle", panel, "Debt Repayment", 28, FontStyle.Bold, Color.black, new Vector2(0f, 170f), new Vector2(310f, 54f));
            CreateText(
                "DebtSummary",
                panel,
                "Remaining: " + FormatMoney(state.debtRemaining) +
                "\nMoney: " + FormatMoney(state.money) +
                "\nDays left: " + daysLeft,
                21,
                FontStyle.Bold,
                Color.black,
                new Vector2(0f, 75f),
                new Vector2(310f, 110f));

            debtInput = CreateInputField("DebtAmountInput", panel, "Amount", new Vector2(0f, -25f), new Vector2(230f, 58f));
            FillDebtInputWithMax();

            CreateDebtActionButton(panel, "PayDebtButton", "Pay Debt", new Vector2(0f, -110f), new Vector2(220f, 62f), TryPayDebtFromInput);
            CreateDebtActionButton(panel, "MaxDebtButton", "Max", new Vector2(0f, -185f), new Vector2(150f, 50f), FillDebtInputWithMax);
        }

        private void CreateDebtActionButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateImage(name, parent, Hex("#F08A5C"), position, size, true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            CreateText(name + "Label", rect, label, 20, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
        }

        private void CreateItemCard(ShopItemData item, Vector2 position, int itemNumber, int itemCount)
        {
            RectTransform card = CreateImage(item.id + "Card", cardRoot, new Color(1f, 1f, 1f, 0.9f), position, new Vector2(340f, 470f), false);
            itemCards.Add(card.gameObject);
            CreateText(item.id + "Counter", card, itemNumber + " / " + itemCount, 18, FontStyle.Bold, Hex("#637064"), new Vector2(0f, 200f), new Vector2(300f, 34f));
            CreateText(item.id + "Name", card, item.displayName, 30, FontStyle.Bold, Color.black, new Vector2(0f, 135f), new Vector2(300f, 60f));
            CreateText(item.id + "Flavor", card, item.flavorText, 20, FontStyle.Normal, Color.black, new Vector2(0f, 45f), new Vector2(290f, 95f));

            float displayPrice = GetDisplayPrice(item);
            bool complete = IsItemComplete(item);
            bool blockedByDebt = item.category == ShopCategory.Decoration && state.debtRemaining > 0f;
            bool canAfford = state.money >= displayPrice && !complete && !blockedByDebt;
            string buttonText = GetPurchaseButtonText(item, complete, blockedByDebt, displayPrice);
            Color buttonColor = complete ? Hex("#C9D2CA") : (canAfford ? Hex("#F08A5C") : Hex("#E5E5E5"));
            RectTransform buyRect = CreateImage(item.id + "Buy", card, buttonColor, new Vector2(0f, -165f), new Vector2(220f, 68f), true);
            Button buyButton = buyRect.gameObject.AddComponent<Button>();
            buyButton.targetGraphic = buyRect.GetComponent<Image>();
            buyButton.interactable = canAfford;
            ShopItemId targetItem = item.id;
            buyButton.onClick.AddListener(() => TryBuy(targetItem));
            CreateText(item.id + "BuyLabel", buyRect, buttonText, 21, FontStyle.Bold, Color.black, Vector2.zero, buyRect.sizeDelta);
        }

        private void CreatePagerButton(string name, string label, Vector2 position, int direction)
        {
            RectTransform rect = CreateImage(name, cardRoot, Hex("#F08A5C"), position, new Vector2(58f, 58f), true);
            itemCards.Add(rect.gameObject);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => MoveSelection(direction));
            CreateText(name + "Label", rect, label, 30, FontStyle.Bold, Color.black, Vector2.zero, rect.sizeDelta);
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
            Texture2D texture = Resources.Load<Texture2D>(ShopBackgroundResourcePath);
            if (texture == null)
            {
                return false;
            }

            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
