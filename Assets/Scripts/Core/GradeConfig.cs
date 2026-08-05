using System;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class GradeConfig
    {
        public Grade grade;
        public int maxErrors;
        public int tipAmount;
        public bool paysBasePrice;
    }
}
