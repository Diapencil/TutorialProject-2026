using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public int id;
        // public int recipeId;
        [TextArea] public List<string> lines;
        [TextArea] public string hintLine;
    }
}
