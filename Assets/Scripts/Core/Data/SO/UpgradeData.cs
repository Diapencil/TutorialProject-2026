using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class UpgradeData
    {
        public int id;
        public string name;
        public List<int> costPerLevel;
        public List<float> timeReduction;
    }
}
