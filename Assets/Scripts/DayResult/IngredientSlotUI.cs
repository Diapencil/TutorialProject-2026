using UnityEngine;
using TMPro;

public class IngredientSlotUI : MonoBehaviour
{
    // 슬롯 안의 텍스트를 연결할 공간입니다.
    public TextMeshProUGUI amountText;

    // 매니저가 이 함수를 부르면서 숫자를 던져주면, 텍스트가 바뀝니다.
    public void SetLogData(int amount)
    {
        amountText.text = $"x {amount}";
    }
}