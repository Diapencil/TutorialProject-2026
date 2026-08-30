using System;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    [Serializable]
    public sealed class BurgerSpriteCatalog
    {
        private const string PattyRawSizzlePath = "BurgerAssembly/PattyAnimations/RawSizzle";
        private const string PattyPressPath = "BurgerAssembly/PattyAnimations/Press";
        private const string PattyFlipPath = "BurgerAssembly/PattyAnimations/Flip";
        private const string PattyCookedSizzlePath = "BurgerAssembly/PattyAnimations/CookedSizzle";
        private const string BaconRawSizzlePath = "BurgerAssembly/BaconAnimations/RawSizzle";
        private const string BaconCookingPath = "BurgerAssembly/BaconAnimations/Cooking";
        private const string BaconCookedSizzlePath = "BurgerAssembly/BaconAnimations/CookedSizzle";

        private Sprite[] pattyRawSizzleFrames;
        private Sprite[] pattyPressFrames;
        private Sprite[] pattyFlipFrames;
        private Sprite[] pattyCookedSizzleFrames;
        private Sprite[] baconRawSizzleFrames;
        private Sprite[] baconCookingFrames;
        private Sprite[] baconCookedSizzleFrames;

        [Header("Shared UI")]
        [SerializeField] private Sprite rectangle;
        [SerializeField] private Sprite circle;
        [SerializeField] private Sprite triangle;
        [SerializeField] private Sprite roundedRectangle;

        [Header("Environment")]
        [SerializeField] private Sprite kitchenStationBackground;

        [Header("Grill - Patty")]
        [SerializeField] private Sprite pattyBall;
        [SerializeField] private Sprite pattyRaw;
        [SerializeField] private Sprite pattyCooked;
        [SerializeField] private Sprite pattyBurnt;
        [SerializeField] private Sprite[] pattyCookingFrames;

        [Header("Grill - Bacon")]
        [SerializeField] private Sprite baconPile;
        [SerializeField] private Sprite baconRaw;
        [SerializeField] private Sprite baconCooked;
        [SerializeField] private Sprite baconBurnt;

        [Header("Grill - Egg")]
        [SerializeField] private Sprite eggCarton;
        [SerializeField] private Sprite eggRaw;
        [SerializeField] private Sprite eggCooked;
        [SerializeField] private Sprite eggBurnt;

        [Header("Assembly")]
        [SerializeField] private Sprite bunBottom;
        [SerializeField] private Sprite bunTop;
        [SerializeField] private Sprite lettuce;
        [SerializeField] private Sprite lettucePile;
        [SerializeField] private Sprite tomato;
        [SerializeField] private Sprite tomatoPile;
        [SerializeField] private Sprite cheese;
        [SerializeField] private Sprite onion;
        [SerializeField] private Sprite onionTray;
        [SerializeField] private Sprite onionPile;
        [SerializeField] private Sprite pickle;
        [SerializeField] private Sprite pickleTray;
        [SerializeField] private Sprite picklePile;
        [SerializeField] private Sprite jalapeno;
        [SerializeField] private Sprite jalapenoTray;
        [SerializeField] private Sprite jalapenoPile;
        [SerializeField] private Sprite ketchup;
        [SerializeField] private Sprite ketchupCursor;
        [SerializeField] private Sprite mustard;
        [SerializeField] private Sprite mustardCursor;
        [SerializeField] private Sprite completedBurger;

        internal static BurgerSpriteCatalog Active { get; private set; }

        public Sprite PattyBall => pattyBall;
        public Sprite KitchenStationBackground => kitchenStationBackground;
        public Sprite PattyRaw => pattyRaw;
        public Sprite PattyCooked => pattyCooked;
        public Sprite PattyBurnt => pattyBurnt;
        public int PattyCookingFrameCount => pattyCookingFrames?.Length ?? 0;
        public int PattyRawSizzleFrameCount => PattyRawSizzleFrames.Length;
        public int PattyPressFrameCount => PattyPressFrames.Length;
        public int PattyFlipFrameCount => PattyFlipFrames.Length;
        public int PattyCookedSizzleFrameCount => PattyCookedSizzleFrames.Length;
        public int BaconRawSizzleFrameCount => BaconRawSizzleFrames.Length;
        public int BaconCookingFrameCount => BaconCookingFrames.Length;
        public int BaconCookedSizzleFrameCount => BaconCookedSizzleFrames.Length;
        public Sprite BaconPile => baconPile;
        public Sprite BaconRaw => baconRaw;
        public Sprite BaconCooked => baconCooked;
        public Sprite BaconBurnt => baconBurnt;
        public Sprite EggCarton => eggCarton;
        public Sprite EggRaw => eggRaw;
        public Sprite EggCooked => eggCooked;
        public Sprite EggBurnt => eggBurnt;
        public Sprite BunBottom => bunBottom;
        public Sprite BunTop => bunTop;
        public Sprite Lettuce => lettuce;
        public Sprite LettucePile => lettucePile;
        public Sprite Tomato => tomato;
        public Sprite TomatoPile => tomatoPile;
        public Sprite Cheese => cheese;
        public Sprite Onion => onion;
        public Sprite OnionTray => onionTray;
        public Sprite OnionPile => onionPile;
        public Sprite Pickle => pickle;
        public Sprite PickleTray => pickleTray;
        public Sprite PicklePile => picklePile;
        public Sprite Jalapeno => jalapeno;
        public Sprite JalapenoTray => jalapenoTray;
        public Sprite JalapenoPile => jalapenoPile;
        public Sprite Ketchup => ketchup;
        public Sprite KetchupCursor => ketchupCursor;
        public Sprite Mustard => mustard;
        public Sprite MustardCursor => mustardCursor;
        public Sprite CompletedBurger => completedBurger;

        public bool IsConfigured =>
            rectangle != null &&
            circle != null &&
            triangle != null &&
            roundedRectangle != null &&
            kitchenStationBackground != null &&
            pattyBall != null &&
            pattyRaw != null &&
            pattyCooked != null &&
            pattyBurnt != null &&
            pattyCookingFrames != null &&
            pattyCookingFrames.Length > 0 &&
            Array.TrueForAll(pattyCookingFrames, sprite => sprite != null) &&
            baconPile != null &&
            baconRaw != null &&
            baconCooked != null &&
            baconBurnt != null &&
            eggCarton != null &&
            eggRaw != null &&
            eggCooked != null &&
            eggBurnt != null &&
            bunBottom != null &&
            bunTop != null &&
            lettuce != null &&
            lettucePile != null &&
            tomato != null &&
            tomatoPile != null &&
            cheese != null &&
            onion != null &&
            onionTray != null &&
            onionPile != null &&
            pickle != null &&
            pickleTray != null &&
            picklePile != null &&
            jalapeno != null &&
            jalapenoTray != null &&
            jalapenoPile != null &&
            ketchup != null &&
            ketchupCursor != null &&
            mustard != null &&
            mustardCursor != null &&
            completedBurger != null;

        public void ConfigureSharedUi(
            Sprite rectangleSprite,
            Sprite circleSprite,
            Sprite triangleSprite,
            Sprite roundedRectangleSprite)
        {
            rectangle = rectangleSprite;
            circle = circleSprite;
            triangle = triangleSprite;
            roundedRectangle = roundedRectangleSprite;
        }

        public void ConfigureEnvironment(Sprite backgroundSprite)
        {
            kitchenStationBackground = backgroundSprite;
        }

        public void ConfigureCooking(
            Sprite pattyBallSprite,
            Sprite rawPattySprite,
            Sprite cookedPattySprite,
            Sprite burntPattySprite,
            Sprite[] cookingPattySprites,
            Sprite baconPileSprite,
            Sprite rawBaconSprite,
            Sprite cookedBaconSprite,
            Sprite burntBaconSprite,
            Sprite eggCartonSprite,
            Sprite rawEggSprite,
            Sprite cookedEggSprite,
            Sprite burntEggSprite)
        {
            pattyBall = pattyBallSprite;
            pattyRaw = rawPattySprite;
            pattyCooked = cookedPattySprite;
            pattyBurnt = burntPattySprite;
            pattyCookingFrames = cookingPattySprites != null
                ? (Sprite[])cookingPattySprites.Clone()
                : Array.Empty<Sprite>();
            baconPile = baconPileSprite;
            baconRaw = rawBaconSprite;
            baconCooked = cookedBaconSprite;
            baconBurnt = burntBaconSprite;
            eggCarton = eggCartonSprite;
            eggRaw = rawEggSprite;
            eggCooked = cookedEggSprite;
            eggBurnt = burntEggSprite;
        }

        public void ConfigureAssembly(
            Sprite bottomBunSprite,
            Sprite topBunSprite,
            Sprite lettuceSprite,
            Sprite lettucePileSprite,
            Sprite tomatoSprite,
            Sprite tomatoPileSprite,
            Sprite cheeseSprite,
            Sprite onionSprite,
            Sprite onionTraySprite,
            Sprite onionPileSprite,
            Sprite pickleSprite,
            Sprite pickleTraySprite,
            Sprite picklePileSprite,
            Sprite jalapenoSprite,
            Sprite jalapenoTraySprite,
            Sprite jalapenoPileSprite,
            Sprite ketchupSprite,
            Sprite ketchupCursorSprite,
            Sprite mustardSprite,
            Sprite mustardCursorSprite,
            Sprite completedBurgerSprite)
        {
            bunBottom = bottomBunSprite;
            bunTop = topBunSprite;
            lettuce = lettuceSprite;
            lettucePile = lettucePileSprite;
            tomato = tomatoSprite;
            tomatoPile = tomatoPileSprite;
            cheese = cheeseSprite;
            onion = onionSprite;
            onionTray = onionTraySprite;
            onionPile = onionPileSprite;
            pickle = pickleSprite;
            pickleTray = pickleTraySprite;
            picklePile = picklePileSprite;
            jalapeno = jalapenoSprite;
            jalapenoTray = jalapenoTraySprite;
            jalapenoPile = jalapenoPileSprite;
            ketchup = ketchupSprite;
            ketchupCursor = ketchupCursorSprite;
            mustard = mustardSprite;
            mustardCursor = mustardCursorSprite;
            completedBurger = completedBurgerSprite;
        }

        public void Activate()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "BurgerSpriteCatalog is missing one or more serialized Sprite references.");
            }

            Active = this;
        }

        internal static BurgerSpriteCatalog RequireActive()
        {
            if (Active == null || !Active.IsConfigured)
            {
                throw new InvalidOperationException(
                    "A configured BurgerSpriteCatalog must be activated before building burger visuals.");
            }

            return Active;
        }

        public Sprite GetShape(SimpleShape shape)
        {
            switch (shape)
            {
                case SimpleShape.Circle: return circle;
                case SimpleShape.Triangle: return triangle;
                case SimpleShape.RoundedRectangle: return roundedRectangle;
                default: return rectangle;
            }
        }

        public Sprite GetIngredient(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return pattyCooked;
                case IngredientType.Bacon: return baconCooked;
                case IngredientType.Egg: return eggCooked;
                case IngredientType.BunBottom: return bunBottom;
                case IngredientType.BunTop: return bunTop;
                case IngredientType.ToppingLettuce: return lettuce;
                case IngredientType.ToppingTomato: return tomato;
                case IngredientType.ToppingCheese: return cheese;
                case IngredientType.ToppingOnion: return onion;
                case IngredientType.ToppingPickle: return pickle;
                case IngredientType.ToppingJalapeno: return jalapeno;
                case IngredientType.SauceKetchup: return ketchup;
                case IngredientType.SauceMustard: return mustard;
                default: return null;
            }
        }

        public Sprite GetTrayIngredient(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return pattyBall;
                case IngredientType.Bacon: return baconPile;
                case IngredientType.Egg: return eggCarton;
                // Assembly tray icons use the same flat sprites as placement.
                // The old pile art has an oblique side-view height that clashes
                // with the strictly top-down cooking station.
                case IngredientType.ToppingLettuce: return lettuce;
                case IngredientType.ToppingTomato: return tomato;
                case IngredientType.ToppingOnion: return onionTray;
                case IngredientType.ToppingPickle: return pickleTray;
                case IngredientType.ToppingJalapeno: return jalapenoTray;
                default: return GetIngredient(type);
            }
        }

        public Sprite GetSauceCursor(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.SauceKetchup: return ketchupCursor;
                case IngredientType.SauceMustard: return mustardCursor;
                default: return null;
            }
        }

        public Sprite GetRawGrillIngredient(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return pattyRaw;
                case IngredientType.Bacon: return baconRaw;
                case IngredientType.Egg: return eggRaw;
                default: return null;
            }
        }

        public Sprite GetInitialGrillIngredient(IngredientType type)
        {
            return type == IngredientType.Patty ? pattyBall : GetRawGrillIngredient(type);
        }

        public Sprite GetPattyCookingFrame(float elapsedSeconds)
        {
            if (PattyCookingFrameCount == 0)
            {
                return pattyRaw;
            }

            int frameIndex = Mathf.FloorToInt(
                Mathf.Max(0f, elapsedSeconds) /
                CookingPrototypeRules.PattyCookingAnimationFrameSeconds) % pattyCookingFrames.Length;
            return pattyCookingFrames[frameIndex] != null
                ? pattyCookingFrames[frameIndex]
                : pattyRaw;
        }

        public Sprite GetPattyRawSizzleFrame(float elapsedSeconds)
        {
            return GetLoopingFrame(PattyRawSizzleFrames, elapsedSeconds, pattyRaw);
        }

        public Sprite GetPattyPressFrame(float elapsedSeconds, float durationSeconds)
        {
            return GetOneShotFrame(PattyPressFrames, elapsedSeconds, durationSeconds, pattyRaw);
        }

        public Sprite GetPattyFlipFrame(float elapsedSeconds)
        {
            return GetOneShotFrame(PattyFlipFrames, elapsedSeconds, CookingPrototypeRules.FlipAnimationSeconds, pattyCooked);
        }

        public Sprite GetPattyCookedSizzleFrame(float elapsedSeconds)
        {
            return GetLoopingFrame(PattyCookedSizzleFrames, elapsedSeconds, pattyCooked);
        }

        public Sprite GetBaconRawSizzleFrame(float elapsedSeconds)
        {
            return GetLoopingFrame(BaconRawSizzleFrames, elapsedSeconds, baconRaw);
        }

        public Sprite GetBaconCookingFrame(float elapsedSeconds, float durationSeconds)
        {
            return GetOneShotFrame(BaconCookingFrames, elapsedSeconds, durationSeconds, baconCooked);
        }

        public Sprite GetBaconCookedSizzleFrame(float elapsedSeconds)
        {
            return GetLoopingFrame(BaconCookedSizzleFrames, elapsedSeconds, baconCooked);
        }

        public Sprite GetCookedGrillIngredient(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return pattyCooked;
                case IngredientType.Bacon: return baconCooked;
                case IngredientType.Egg: return eggCooked;
                default: return null;
            }
        }

        public Sprite GetBurntGrillIngredient(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return pattyBurnt;
                case IngredientType.Bacon: return baconBurnt;
                case IngredientType.Egg: return eggBurnt;
                default: return null;
            }
        }

        private Sprite[] PattyRawSizzleFrames =>
            pattyRawSizzleFrames ?? (pattyRawSizzleFrames = LoadAnimationFrames(PattyRawSizzlePath));

        private Sprite[] PattyPressFrames =>
            pattyPressFrames ?? (pattyPressFrames = LoadAnimationFrames(PattyPressPath));

        private Sprite[] PattyFlipFrames =>
            pattyFlipFrames ?? (pattyFlipFrames = LoadAnimationFrames(PattyFlipPath));

        private Sprite[] PattyCookedSizzleFrames =>
            pattyCookedSizzleFrames ?? (pattyCookedSizzleFrames = LoadAnimationFrames(PattyCookedSizzlePath));

        private Sprite[] BaconRawSizzleFrames =>
            baconRawSizzleFrames ?? (baconRawSizzleFrames = LoadAnimationFrames(BaconRawSizzlePath));

        private Sprite[] BaconCookingFrames =>
            baconCookingFrames ?? (baconCookingFrames = LoadAnimationFrames(BaconCookingPath));

        private Sprite[] BaconCookedSizzleFrames =>
            baconCookedSizzleFrames ?? (baconCookedSizzleFrames = LoadAnimationFrames(BaconCookedSizzlePath));

        private static Sprite GetLoopingFrame(Sprite[] frames, float elapsedSeconds, Sprite fallback)
        {
            if (frames == null || frames.Length == 0)
            {
                return fallback;
            }

            int frameIndex = Mathf.FloorToInt(
                Mathf.Max(0f, elapsedSeconds) /
                CookingPrototypeRules.PattyCookingAnimationFrameSeconds) % frames.Length;
            return frames[frameIndex] != null ? frames[frameIndex] : fallback;
        }

        private static Sprite GetOneShotFrame(Sprite[] frames, float elapsedSeconds, float durationSeconds, Sprite fallback)
        {
            if (frames == null || frames.Length == 0)
            {
                return fallback;
            }

            float normalized = Mathf.Clamp01(Mathf.Max(0f, elapsedSeconds) / Mathf.Max(0.0001f, durationSeconds));
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalized * frames.Length), 0, frames.Length - 1);
            return frames[frameIndex] != null ? frames[frameIndex] : fallback;
        }

        private static Sprite[] LoadAnimationFrames(string resourcesPath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcesPath);
            if (frames == null || frames.Length == 0)
            {
                return Array.Empty<Sprite>();
            }

            Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));
            return frames;
        }
    }
}
