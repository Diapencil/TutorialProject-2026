using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro를 조작하기 위해 반드시 필요합니다!
using UnityEngine.SceneManagement; // 씬(Scene) 이동을 위해 필요합니다!
using SheepSheepBurger.Core;

public class DayResultManager : MonoBehaviour
{
    [Header("기본 통계 UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI perfectText;
    public TextMeshProUGUI goodText;
    public TextMeshProUGUI normalText;
    public TextMeshProUGUI badText;
    public TextMeshProUGUI dDayText;

    [Header("로그 생성용 변수")]
    public GameObject ingredientPrefab;
    public Transform gridParent;

    void Start()
    {
        GameManager gm = GameManager.GetOrCreate();
        GameState currentState = gm.State;

        titleText.text = $"Day {currentState.currentDay}!";

        int leftDays = currentState.debtDeadline - currentState.currentDay;
        dDayText.text = $"D-day: {leftDays}";
    }

    // 통계 데이터를 받아서 화면의 글씨를 바꿔주는 함수
    public void UpdateResultUI(int day, int money, int perfect, int good, int normal, int bad, int dDay)
    {
        titleText.text = $"Day {day}!";
        moneyText.text = $"Today's Revenue: {money} Canine";
        perfectText.text = $"Perfect: {perfect}";
        goodText.text = $"Good: {good}";
        normalText.text = $"Normal: {normal}";
        badText.text = $"Bad: {bad}";
        dDayText.text = $"D-day: {dDay}";
    }

    // 'Next Day' 버튼을 눌렀을 때 실행될 함수 (버튼에 연결하려면 반드시 public이어야 합니다)
    public void OnNextDayButtonClicked()
    {
        // 다음 장사를 시작하는 씬으로 넘어갑니다. (실제 씬 이름으로 바꿔주세요)
        SceneManager.LoadScene("MainGame");
    }

    public void CreateIngredientLogs(List<int> logList)
    {
        // 받은 로그 리스트를 하나씩 꺼내보면서 반복합니다.
        foreach (int usedAmount in logList)
        {
            // 1. 프리팹을 복사해서 gridParent(바둑판) 아래에 생성합니다.
            GameObject newSlot = Instantiate(ingredientPrefab, gridParent);

            // 2. 방금 만든 슬롯에 달려있는 IngredientSlotUI 스크립트를 가져옵니다.
            IngredientSlotUI slotScript = newSlot.GetComponent<IngredientSlotUI>();

            // 3. 스크립트가 잘 있다면, 사용한 개수(usedAmount)를 전달해 글씨를 바꿉니다.
            if (slotScript != null)
            {
                slotScript.SetLogData(usedAmount);
            }
        }
    }