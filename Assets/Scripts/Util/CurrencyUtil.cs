// 10배 정수로 저장된 금액과 화면 표시 문자열 사이를 변환한다.
using System.Globalization;
using UnityEngine;

namespace SheepSheepBurger.Util
{
    /// <summary>
    /// 모든 금액은 int로 저장하되 실제 값의 10배로 관리한다. 화면에 낼 때만 10으로 나눈다.
    /// 확률/시간/배율(float)은 이 규칙에서 제외한다.
    /// </summary>
    public static class CurrencyUtil
    {
        /// <summary>저장값(10배 정수) → 표시 문자열. 50 → "5.0C"</summary>
        public static string ToDisplay(int stored)
        {
            return (stored / 10f).ToString("0.0", CultureInfo.InvariantCulture) + "C";
        }

        /// <summary>실제 금액 → 저장값(10배 정수). 5.0 → 50</summary>
        public static int ToStored(float actual)
        {
            return Mathf.RoundToInt(actual * 10f);
        }
    }
}
