using System.Collections.Generic;
using UnityEngine;

// 손님 등급을 정의하는 열거형 (인스펙터에서 드롭다운으로 선택 가능)
public enum CustomerGrade
{
    Normal,    // 기본
    Advanced,  // 고급
    Special,   // 특수
    Event      // 이벤트
}

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "SheepBurger/Customer Data", order = 2)]
public class CustomerData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("시스템 고유 식별자 (예: Cust_Wolf)")]
    public string customerID;

    [Tooltip("인게임에 표시될 손님 이름 (예: 평범한 늑대)")]
    public string customerName;

    [Tooltip("손님의 등급 (기본/고급/특수/이벤트)")]
    public CustomerGrade grade;

    [Header("주문 정보")]
    [Tooltip("이 손님이 주문할 가능성이 있는 메뉴 목록")]
    public List<RecipeData> preferredMenus;

    [Tooltip("주문 후 기다려주는 제한 시간 (초)")]
    public float patienceTime = 30f;

    [Header("생존 리스크 시스템")]
    [Range(0f, 1f)]
    [Tooltip("만족도 미달 시 플레이어를 잡아먹을 확률 (0.0 ~ 1.0)")]
    public float eatProbability;

    [Tooltip("잡아먹혔을 때 차감되는 금액")]
    public int penaltyCost;

    [Header("비주얼 리소스")]
    [Tooltip("게임 내에서 보여질 손님의 도트 그래픽(스프라이트)")]
    public Sprite visualAsset;
}