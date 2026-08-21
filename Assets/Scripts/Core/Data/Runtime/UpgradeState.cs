// 설비 업그레이드의 런타임 진행 상태. JsonUtility 호환을 위해 Dictionary 대신 리스트 원소로 쓴다.
using System;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class UpgradeState
    {
        public int id;
        public int currentLevel;
    }
}
