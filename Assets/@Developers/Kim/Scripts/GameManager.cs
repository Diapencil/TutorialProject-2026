using System.Collections.Generic;
using UnityEngine;

// 씬 사이에서 주문과 결과를 보관하는 싱글톤입니다.
// 붙이는 오브젝트: 직접 붙이지 않아도 됩니다. 실행 시 자동 생성되고 DontDestroyOnLoad로 유지됩니다.
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool HasCurrentOrder { get; private set; }
    public RecipeId CurrentOrderId { get; private set; }
    public bool HasLastResult { get; private set; }
    public bool LastSuccess { get; private set; }
    public string LastOrderName { get; private set; }
    public string LastMadeBurgerName { get; private set; }

    private int orderCursor = -1;

    public Recipe CurrentOrder => RecipeBook.Get(CurrentOrderId);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static GameManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("GameManager");
        Instance = managerObject.AddComponent<GameManager>();
        DontDestroyOnLoad(managerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 다음 손님 주문을 순서대로 뽑습니다. 반복 테스트가 쉽도록 4개 레시피를 순환합니다.
    public Recipe PickNextOrder()
    {
        IReadOnlyList<Recipe> recipes = RecipeBook.All;
        orderCursor = (orderCursor + 1) % recipes.Count;
        CurrentOrderId = recipes[orderCursor].Id;
        HasCurrentOrder = true;
        HasLastResult = false;
        LastOrderName = recipes[orderCursor].DisplayName;
        LastMadeBurgerName = string.Empty;
        return recipes[orderCursor];
    }

    public void EnsureOrder()
    {
        if (!HasCurrentOrder)
        {
            PickNextOrder();
        }
    }

    // CookingScene 판정 결과를 저장합니다.
    public void SaveCookingResult(bool success, Recipe matchedRecipe)
    {
        HasLastResult = true;
        LastSuccess = success;
        LastOrderName = CurrentOrder.DisplayName;
        LastMadeBurgerName = matchedRecipe != null ? matchedRecipe.DisplayName : "알 수 없는 버거";
    }

    public string BuildResultMessage()
    {
        if (!HasLastResult)
        {
            return "아직 완성한 버거가 없습니다.";
        }

        if (LastSuccess)
        {
            return "성공! " + LastMadeBurgerName + "를 완성했습니다";
        }

        return "실패... 만든 것: " + LastMadeBurgerName + "\n다시 만들어볼까요?";
    }
}
