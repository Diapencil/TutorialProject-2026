using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class Upgrade
    {
        public int id;
        public string name;
        public int currentLevel;
        public List<int> costPerLevel;
        public List<float> timeReduction;
    }
}
