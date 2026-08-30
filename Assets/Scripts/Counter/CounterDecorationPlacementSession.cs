using SheepSheepBurger.Core;

namespace SheepSheepBurger.Counter
{
    public static class CounterDecorationPlacementSession
    {
        public static DecorationData PendingDecoration { get; private set; }

        public static bool HasPendingDecoration => PendingDecoration != null;

        public static void Begin(DecorationData decoration)
        {
            PendingDecoration = decoration;
        }

        public static DecorationData Consume()
        {
            DecorationData decoration = PendingDecoration;
            PendingDecoration = null;
            return decoration;
        }
    }
}
