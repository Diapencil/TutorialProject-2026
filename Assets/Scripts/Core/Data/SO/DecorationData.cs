// 카운터 화면에 배치되는 장식 아이템 한 종류의 정적 데이터.
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheep/Decoration Data")]
    public class DecorationData : ScriptableObject
    {
        public int id;
        public string decorationName;
        public Sprite sprite;

        /// <summary>실제 금액의 10배로 저장된 구매 비용.</summary>
        public int cost;

        /// <summary>카운터 화면에서의 배치 좌표.</summary>
        public Vector2 counterPosition;
    }
}
