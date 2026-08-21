// 설비(튀김기/그릴판) 한 종류의 정적 데이터. 현재 레벨은 이 애셋이 아니라 GameState에서만 관리한다.
using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public int id;
        public new string name;
        public List<int> costPerLevel = new List<int>();
        public List<float> timeReduction = new List<float>();

        public UpgradeType type;

        /// <summary>상점 슬롯에 표시할 아이콘. 아트 리소스 입고 전까지는 비워둔다.</summary>
        public Sprite icon;

        public int maxLevel = 4;

        /// <summary>그릴판 전용. 레벨별 탄 확률.</summary>
        public List<float> burnChancePerLevel = new List<float>();
    }
}
