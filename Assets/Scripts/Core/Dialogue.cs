using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public int id;
        public int recipeId;
        public List<string> lines;
        public string hintLine;
    }
}
