using System;
using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(fileName = "GradeConfig", menuName = "SheepSheepBurger/Grade Config")]
    public class GradeConfig : ScriptableObject
    {
        [Tooltip("Grade settings, typically Perfect, Good, Normal, and Bad.")]
        public List<GradeEntry> grades = new List<GradeEntry>(4);
    }

    [Serializable]
    public class GradeEntry
    {
        public Grade grade;
        public int maxErrors;
        public int tipAmount;
        public bool paysBasePrice;
    }
}
