using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipeData", menuName = "SheepBurger/Recipe Data", order = 1)]
public class RecipeData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("시스템에서 식별할 고유 ID (예: Burger_Basic)")]
    public string recipeID;

    [Tooltip("인게임 UI에 표시될 이름 (예: 고기 듬뿍 버거)")]
    public string recipeName;

    [Header("상세 정보")]
    [Tooltip("요리에 필요한 재료 목록")]
    public List<string> ingredients; // ※ 추후 재료도 ScriptableObject나 Enum으로 관리하면 더 좋습니다.

    [Tooltip("요리 성공 시 획득할 골드")]
    public int price;

    [Tooltip("해금 조건 (예: 특정 스테이지 클리어)")]
    public string unlockCondition;
}