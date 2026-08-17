using System;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    [Serializable]
    public sealed class BurgerSpriteCatalog
    {
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
        [SerializeField] private Sprite onionPile;
        [SerializeField] private Sprite pickle;
        [SerializeField] private Sprite picklePile;
        [SerializeField] private Sprite jalapeno;
        [SerializeField] private Sprite jalapenoPile;
        [SerializeField] private Sprite ketchup;
        [SerializeField] private Sprite mustard;
        [SerializeField] private Sprite completedBurger;

        internal static BurgerSpriteCatalog Active { get; private set; }

        public Sprite PattyBall => pattyBall;
        public Sprite KitchenStationBackground => kitchenStationBackground;
        public Sprite PattyRaw => pattyRaw;
        public Sprite PattyCooked => pattyCooked;
        public Sprite PattyBurnt => pattyBurnt;
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
        public Sprite OnionPile => onionPile;
        public Sprite Pickle => pickle;
        public Sprite PicklePile => picklePile;
        public Sprite Jalapeno => jalapeno;
        public Sprite JalapenoPile => jalapenoPile;
        public Sprite Ketchup => ketchup;
        public Sprite Mustard => mustard;
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
            onionPile != null &&
            pickle != null &&
            picklePile != null &&
            jalapeno != null &&
            jalapenoPile != null &&
            ketchup != null &&
            mustard != null &&
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
            Sprite onionPileSprite,
            Sprite pickleSprite,
            Sprite picklePileSprite,
            Sprite jalapenoSprite,
            Sprite jalapenoPileSprite,
            Sprite ketchupSprite,
            Sprite mustardSprite,
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
            onionPile = onionPileSprite;
            pickle = pickleSprite;
            picklePile = picklePileSprite;
            jalapeno = jalapenoSprite;
            jalapenoPile = jalapenoPileSprite;
            ketchup = ketchupSprite;
            mustard = mustardSprite;
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
                case IngredientType.ToppingOnion: return onion;
                case IngredientType.ToppingPickle: return pickle;
                case IngredientType.ToppingJalapeno: return jalapeno;
                default: return GetIngredient(type);
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
    }
}
