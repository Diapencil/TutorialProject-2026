using Lee.Counter;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CookingTestSubmitter : MonoBehaviour
{
    public void SubmitPerfectBurger(int x)
    {
        OrderData order = CounterSceneSession.ActiveOrder;

        if (order == null)
        {
            Debug.LogError("활성 주문이 없습니다.");
            return;
        }

        BurgerData burger;
        switch (x)
        {
            case 2: 
                burger = new BurgerData(order.RequestedRecipe.Ingredients);
                break;
            case 1:
                burger = new BurgerData(new[]
                {
                    IngredientType.BunBottom,
                    IngredientType.Patty,
                    IngredientType.BunTop
                }); 
                break;
            default:
                burger = new BurgerData(new[]
                {
                    IngredientType.BunBottom,
                    IngredientType.BunTop
                });
                break;
        }

        CounterSceneSession.SubmitCookedBurger(burger);
        SceneManager.LoadScene("Counter");
    }
}