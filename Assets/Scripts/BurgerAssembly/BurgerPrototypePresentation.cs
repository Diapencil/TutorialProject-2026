using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    internal static class BurgerPrototypeTheme
    {
        public static readonly Color Background = Hex("#E8E2D2");
        public static readonly Color Ink = Hex("#263027");
        public static readonly Color Border = Hex("#30382E");
        public static readonly Color GrillZone = Hex("#6D3F333D");
        public static readonly Color BoardZone = Hex("#D6C79C30");
        public static readonly Color PackagingZone = Hex("#63705B42");
        // The station illustration provides the physical structure. UI surfaces
        // should read as light controls placed on the counter, not a second set.
        public static readonly Color Panel = Hex("#F4EBD6B8");
        public static readonly Color Card = Hex("#FFF9EAE8");
        public static readonly Color Guide = Hex("#F4E1B5C4");
        public static readonly Color Board = Hex("#F1DCAFD9");
        public static readonly Color BoardEdge = Hex("#66715E");
        public static readonly Color Grill = Hex("#5B392FE8");
        public static readonly Color GrillBar = Hex("#A28B6B");
        public static readonly Color Accent = Hex("#6B7762");
        public static readonly Color RawPatty = Hex("#C86B61");
        public static readonly Color CookedPatty = Hex("#754129");
        public static readonly Color BurntPatty = Hex("#211B18");
        public static readonly Color Bun = Hex("#E8A64C");
        public static readonly Color Warning = Hex("#A33A2B");
        public static readonly Color Attention = Hex("#E38B22");
        public static readonly Color Success = Hex("#287A3A");

        public static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.magenta;
        }
    }

    internal readonly struct BurgerIngredientVisual
    {
        public BurgerIngredientVisual(SimpleShape shape, Color color, Vector2 size, Sprite sourceSprite)
        {
            Shape = shape;
            Color = color;
            Size = size;
            SourceSprite = sourceSprite;
        }

        public SimpleShape Shape { get; }

        public Color Color { get; }

        public Vector2 Size { get; }

        public Sprite SourceSprite { get; }
    }

    internal readonly struct BurgerTrayItemDefinition
    {
        public BurgerTrayItemDefinition(
            string objectName,
            string label,
            CookingDragKind kind,
            IngredientType type,
            Vector2 position,
            Vector2 cardSize)
        {
            ObjectName = objectName;
            Label = label;
            Kind = kind;
            Type = type;
            Position = position;
            CardSize = cardSize;
        }

        public string ObjectName { get; }

        public string Label { get; }

        public CookingDragKind Kind { get; }

        public IngredientType Type { get; }

        public Vector2 Position { get; }

        public Vector2 CardSize { get; }
    }

    internal static class BurgerIngredientCatalog
    {
        private const float BoardIngredientReferenceSize = 100f;
        private static readonly Vector2 BoardIngredientReference = new Vector2(
            BoardIngredientReferenceSize,
            BoardIngredientReferenceSize);
        private static readonly Vector2 BoardBunSize = BoardIngredientReference * 0.9f;
        private static readonly Vector2 BoardMediumToppingSize = BoardIngredientReference * 0.8f;
        private static readonly Vector2 BoardSmallToppingSize = BoardIngredientReference * 0.5f;

        private static readonly BurgerTrayItemDefinition[] BoardTrayItems =
        {
            new BurgerTrayItemDefinition("BottomBunTray", "하단 번", CookingDragKind.Ingredient, IngredientType.BunBottom, new Vector2(-360f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("TopBunTray", "상단 번", CookingDragKind.Ingredient, IngredientType.BunTop, new Vector2(-240f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("LettuceTray", "양상추", CookingDragKind.Ingredient, IngredientType.ToppingLettuce, new Vector2(-120f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("TomatoTray", "토마토", CookingDragKind.Ingredient, IngredientType.ToppingTomato, new Vector2(0f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("JalapenoTray", "할라피뇨", CookingDragKind.Ingredient, IngredientType.ToppingJalapeno, new Vector2(120f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("OnionTray", "양파", CookingDragKind.Ingredient, IngredientType.ToppingOnion, new Vector2(240f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("PickleTray", "피클", CookingDragKind.Ingredient, IngredientType.ToppingPickle, new Vector2(360f, 90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("KetchupTray", "케첩", CookingDragKind.Sauce, IngredientType.SauceKetchup, new Vector2(-360f, -90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("MustardTray", "머스터드", CookingDragKind.Sauce, IngredientType.SauceMustard, new Vector2(-240f, -90f), new Vector2(100f, 100f)),
            new BurgerTrayItemDefinition("CheeseTray", "치즈", CookingDragKind.Ingredient, IngredientType.ToppingCheese, new Vector2(-120f, -90f), new Vector2(100f, 100f))
        };

        public static IReadOnlyList<BurgerTrayItemDefinition> GetBoardTrayItems()
        {
            return BoardTrayItems;
        }

        public static string GetDisplayName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return "구운 패티";
                case IngredientType.Bacon: return "구운 베이컨";
                case IngredientType.Egg: return "계란후라이";
                case IngredientType.BunBottom: return "하단 번";
                case IngredientType.BunTop: return "상단 번";
                case IngredientType.ToppingLettuce: return "양상추";
                case IngredientType.ToppingTomato: return "토마토";
                case IngredientType.ToppingCheese: return "치즈";
                case IngredientType.ToppingOnion: return "양파";
                case IngredientType.ToppingPickle: return "피클";
                case IngredientType.ToppingJalapeno: return "할라피뇨";
                case IngredientType.SauceKetchup: return "케첩";
                case IngredientType.SauceMustard: return "머스터드";
                default: return type.ToString();
            }
        }

        public static BurgerIngredientVisual GetVisual(IngredientType type)
        {
            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            switch (type)
            {
                case IngredientType.Patty:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.CookedPatty, BoardIngredientReference, sprites.PattyCooked);
                case IngredientType.Bacon:
                    return new BurgerIngredientVisual(SimpleShape.Rectangle, BurgerPrototypeTheme.Hex("#B96C5C"), new Vector2(285f, 150f), sprites.BaconCooked);
                case IngredientType.Egg:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#FFF1B8"), new Vector2(245f, 245f), sprites.EggCooked);
                case IngredientType.BunBottom:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Bun, BoardBunSize, sprites.BunBottom);
                case IngredientType.BunTop:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Bun, BoardBunSize, sprites.BunTop);
                case IngredientType.ToppingLettuce:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#63B94D"), BoardIngredientReference, sprites.Lettuce);
                case IngredientType.ToppingTomato:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#E84B3C"), BoardMediumToppingSize, sprites.Tomato);
                case IngredientType.ToppingCheese:
                    return new BurgerIngredientVisual(SimpleShape.Rectangle, BurgerPrototypeTheme.Hex("#FFD84C"), new Vector2(310f, 310f), sprites.Cheese);
                case IngredientType.ToppingOnion:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#B981C7"), BoardMediumToppingSize, sprites.Onion);
                case IngredientType.ToppingPickle:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#7C9B55"), BoardSmallToppingSize, sprites.Pickle);
                case IngredientType.ToppingJalapeno:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#6A9B52"), BoardSmallToppingSize, sprites.Jalapeno);
                case IngredientType.SauceKetchup:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#D92E28"), new Vector2(22f, 22f), sprites.Ketchup);
                case IngredientType.SauceMustard:
                    return new BurgerIngredientVisual(SimpleShape.Circle, BurgerPrototypeTheme.Hex("#F5C542"), new Vector2(22f, 22f), sprites.Mustard);
                default:
                    return new BurgerIngredientVisual(SimpleShape.Rectangle, Color.white, new Vector2(80f, 50f), sprites.GetShape(SimpleShape.Rectangle));
            }
        }

        public static BurgerIngredientVisual GetTrayVisual(IngredientType type)
        {
            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            switch (type)
            {
                case IngredientType.Patty:
                    return new BurgerIngredientVisual(
                        SimpleShape.Circle,
                        BurgerPrototypeTheme.RawPatty,
                        new Vector2(150f, 150f),
                        sprites.PattyBall);
                case IngredientType.Bacon:
                    return new BurgerIngredientVisual(
                        SimpleShape.Rectangle,
                        BurgerPrototypeTheme.Hex("#E7A69D"),
                        new Vector2(285f, 125f),
                        sprites.BaconPile);
                case IngredientType.Egg:
                    return new BurgerIngredientVisual(
                        SimpleShape.Rectangle,
                        BurgerPrototypeTheme.Hex("#FFF1D8"),
                        new Vector2(230f, 125f),
                        sprites.EggCarton);
                case IngredientType.SauceKetchup:
                    return new BurgerIngredientVisual(
                        SimpleShape.Rectangle,
                        BurgerPrototypeTheme.Hex("#D92E28"),
                        new Vector2(100f, 110f),
                        sprites.Ketchup);
                case IngredientType.SauceMustard:
                    return new BurgerIngredientVisual(
                        SimpleShape.Rectangle,
                        BurgerPrototypeTheme.Hex("#F5C542"),
                        new Vector2(100f, 110f),
                        sprites.Mustard);
                case IngredientType.ToppingLettuce:
                case IngredientType.ToppingTomato:
                case IngredientType.ToppingOnion:
                case IngredientType.ToppingPickle:
                case IngredientType.ToppingJalapeno:
                {
                    BurgerIngredientVisual placement = GetVisual(type);
                    return new BurgerIngredientVisual(
                        placement.Shape,
                        placement.Color,
                        placement.Size,
                        sprites.GetTrayIngredient(type));
                }
                default:
                    return GetVisual(type);
            }
        }

        public static Sprite GetSauceBrushSprite()
        {
            return BurgerSpriteCatalog.RequireActive().GetShape(SimpleShape.Circle);
        }

        public static bool IsTopping(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.ToppingLettuce:
                case IngredientType.ToppingTomato:
                case IngredientType.ToppingCheese:
                case IngredientType.ToppingOnion:
                case IngredientType.ToppingPickle:
                case IngredientType.ToppingJalapeno:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSauce(IngredientType type)
        {
            return type == IngredientType.SauceKetchup || type == IngredientType.SauceMustard;
        }

        public static bool IsGrillIngredient(IngredientType type)
        {
            return type == IngredientType.Patty ||
                type == IngredientType.Bacon ||
                type == IngredientType.Egg;
        }

        public static bool RequiresFlip(IngredientType type)
        {
            return type == IngredientType.Patty || type == IngredientType.Bacon;
        }
    }

    internal static class BurgerUiFactory
    {
        public static RectTransform CreateImage(
            string name,
            RectTransform parent,
            Color color,
            Vector2 position,
            Vector2 size,
            bool raycastTarget)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        public static SimpleShapeGraphic CreateShape(
            string name,
            RectTransform parent,
            SimpleShape shape,
            Color color,
            Vector2 position,
            Vector2 size,
            bool raycastTarget,
            Sprite sourceSprite = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(SimpleShapeGraphic));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            SimpleShapeGraphic graphic = gameObject.GetComponent<SimpleShapeGraphic>();
            graphic.Shape = shape;
            graphic.SourceSprite = sourceSprite;
            graphic.color = sourceSprite == null ? color : Color.white;
            graphic.raycastTarget = raycastTarget;
            return graphic;
        }

        public static SimpleShapeGraphic RebuildTrayVisualPile(
            Transform traySource,
            string sourceName,
            BurgerIngredientVisual visual,
            Vector2 traySize,
            bool usePile)
        {
            RectTransform root = FindDirectChild<RectTransform>(traySource, sourceName + "VisualPile");
            if (root == null)
            {
                var rootObject = new GameObject(sourceName + "VisualPile", typeof(RectTransform));
                root = rootObject.GetComponent<RectTransform>();
                root.SetParent(traySource, false);
            }

            SetRect(root, Vector2.zero, traySize);
            ClearChildren(root);
            DestroyDirectChild(traySource, sourceName + "Icon");

            Vector2 iconSize = GetTraySingleIconSize(visual.Size, traySize);
            if (!usePile)
            {
                return CreateTrayPileIcon(
                    root,
                    sourceName + "Icon",
                    visual,
                    Vector2.zero,
                    iconSize,
                    0f);
            }

            SimpleShapeGraphic main = null;
            Vector2 offsetScale = new Vector2(traySize.x / 100f, traySize.y / 100f);
            for (int i = 0; i < TrayPileOffsets.Length; i++)
            {
                string iconName = i == TrayPileOffsets.Length - 1
                    ? sourceName + "Icon"
                    : sourceName + "Pile" + i.ToString("00");
                Vector2 position = new Vector2(
                    TrayPileOffsets[i].x * offsetScale.x,
                    TrayPileOffsets[i].y * offsetScale.y);
                SimpleShapeGraphic icon = CreateTrayPileIcon(
                    root,
                    iconName,
                    visual,
                    position,
                    iconSize * TrayPileScales[i],
                    TrayPileRotations[i]);
                icon.color = visual.SourceSprite == null
                    ? visual.Color
                    : new Color(1f, 1f, 1f, TrayPileAlphas[i]);
                main = icon;
            }

            main.transform.SetAsLastSibling();
            return main;
        }

        public static Text CreateText(
            string name,
            RectTransform parent,
            Font font,
            string value,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, dimensions);
            Text text = gameObject.GetComponent<Text>();
            text.font = font;
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

        public static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static SimpleShapeGraphic CreateTrayPileIcon(
            RectTransform parent,
            string name,
            BurgerIngredientVisual visual,
            Vector2 position,
            Vector2 size,
            float rotation)
        {
            SimpleShapeGraphic icon = CreateShape(
                name,
                parent,
                visual.Shape,
                visual.Color,
                position,
                size,
                false,
                visual.SourceSprite);
            icon.color = visual.SourceSprite == null ? visual.Color : Color.white;
            icon.transform.localEulerAngles = new Vector3(0f, 0f, rotation);
            return icon;
        }

        private static readonly Vector2[] TrayPileOffsets =
        {
            new Vector2(-37f, 20f),
            new Vector2(-22f, 31f),
            new Vector2(4f, 27f),
            new Vector2(29f, 23f),
            new Vector2(39f, 2f),
            new Vector2(25f, -18f),
            new Vector2(6f, -30f),
            new Vector2(-20f, -27f),
            new Vector2(-39f, -8f),
            new Vector2(-28f, 4f),
            new Vector2(-9f, 13f),
            new Vector2(14f, 10f),
            new Vector2(27f, -3f),
            new Vector2(8f, -10f),
            new Vector2(-12f, -12f),
            new Vector2(-1f, 2f),
            new Vector2(17f, -26f),
            new Vector2(-31f, -22f),
            new Vector2(32f, 32f),
            new Vector2(0f, -1f)
        };

        private static readonly float[] TrayPileScales =
        {
            0.54f, 0.62f, 0.57f, 0.6f, 0.52f,
            0.66f, 0.58f, 0.63f, 0.55f, 0.7f,
            0.78f, 0.74f, 0.69f, 0.83f, 0.8f,
            0.92f, 0.61f, 0.56f, 0.5f, 1f
        };

        private static readonly float[] TrayPileRotations =
        {
            -24f, 11f, -8f, 19f, -17f,
            28f, -31f, 15f, 36f, -5f,
            22f, -18f, 8f, -27f, 14f,
            -7f, 31f, -34f, 24f, 4f
        };

        private static readonly float[] TrayPileAlphas =
        {
            0.74f, 0.78f, 0.75f, 0.77f, 0.72f,
            0.82f, 0.76f, 0.8f, 0.73f, 0.84f,
            0.88f, 0.86f, 0.85f, 0.9f, 0.89f,
            0.94f, 0.79f, 0.74f, 0.7f, 1f
        };

        private static Vector2 GetTraySingleIconSize(Vector2 visualSize, Vector2 traySize)
        {
            float iconLimit = Mathf.Min(78f, traySize.y * 0.72f);
            float iconScale = Mathf.Min(
                iconLimit / Mathf.Max(1f, visualSize.x),
                iconLimit / Mathf.Max(1f, visualSize.y));
            return visualSize * iconScale;
        }

        private static T FindDirectChild<T>(Transform parent, string childName) where T : Component
        {
            if (parent == null) return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName) return child.GetComponent<T>();
            }

            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyObject(parent.GetChild(i).gameObject);
            }
        }

        private static void DestroyDirectChild(Transform parent, string childName)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    DestroyObject(child.gameObject);
                }
            }
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        public static Vector2 ClampInside(Rect bounds, Vector2 local, Vector2 itemSize)
        {
            float halfWidth = Mathf.Min(bounds.width * 0.5f, itemSize.x * 0.5f);
            float halfHeight = Mathf.Min(bounds.height * 0.5f, itemSize.y * 0.5f);
            return new Vector2(
                Mathf.Clamp(local.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth),
                Mathf.Clamp(local.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight));
        }
    }
}
