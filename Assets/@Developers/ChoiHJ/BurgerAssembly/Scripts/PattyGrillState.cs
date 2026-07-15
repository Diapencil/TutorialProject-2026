namespace SheepSheepBurger.BurgerAssembly
{
    public enum PattyGrillPhase
    {
        Empty,
        Raw,
        Cooked
    }

    public sealed class PattyGrillState
    {
        public PattyGrillPhase Phase { get; private set; } = PattyGrillPhase.Empty;

        public bool TryLoadRawPatty()
        {
            if (Phase != PattyGrillPhase.Empty)
            {
                return false;
            }

            Phase = PattyGrillPhase.Raw;
            return true;
        }

        public bool TryCook()
        {
            if (Phase != PattyGrillPhase.Raw)
            {
                return false;
            }

            Phase = PattyGrillPhase.Cooked;
            return true;
        }

        public bool TryTakeCookedPatty()
        {
            if (Phase != PattyGrillPhase.Cooked)
            {
                return false;
            }

            Phase = PattyGrillPhase.Empty;
            return true;
        }

        public void Reset()
        {
            Phase = PattyGrillPhase.Empty;
        }
    }
}
