using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [TextArea] public List<string> orderLines;
        [TextArea] public string hintLine;
        [TextArea] public string perfectLine;
        // [TextArea] public string goodLine;
        [TextArea] public string normalLine;
        [TextArea] public string badLine;
    }
}
