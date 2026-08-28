namespace SheepSheepBurger.BurgerAssembly
{
    public static class CookingPrototypeRules
    {
        // Temporary visual guide for tuning the three interaction regions.
        // Set this to false when the regions no longer need to be displayed.
        public const bool ShowTemporaryInteractionAreas = false;

        public const float FirstSideCookSeconds = 3f;
        public const float CookingTimeLimitSeconds = 60f;
        public const float PattyCookingAnimationFrameSeconds = 0.19f;
        public const float ReadyToFlipBurnSeconds = 5f;
        public const float SecondSideCookSeconds = 3f;
        public const float FlipAnimationSeconds = 0.25f;
        public const float DoneToOvercookedSeconds = 5f;
        public const float CameraTweenSeconds = 0.35f;
        public const float DragThresholdPixels = 5f;
        public const float SauceStampSpacingPixels = 10f;
        public const float SauceBurgerAttachPadding = 44f;
        public const float PattyEdgeTransferScreenRatio = 0.9f;
        public const float CompletedBurgerTransferScreenRatio = 0.9f;
        public const float BurgerStackSnapPadding = 80f;
        public const float BurgerStackUnadjustedRadiusRatio = 0.65f;
        public const float BurgerStackEdgeCorrectionDistance = 28f;
        public const float BoardToGrillTransferScreenRatio = 0.1f;
        public const int MaximumToppings = 8;
        public const int MaximumSaucePointsPerStroke = 4096;
    }
}
