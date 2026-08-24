using System.Collections.Generic;
using SheepSheepBurger.Counter;
using SheepSheepBurger.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CookingTestSubmitter : MonoBehaviour
{
    public void SubmitPerfectBurger(int quality)
    {
        var activeOrder = CounterSceneSession.ActiveOrder;
        if (activeOrder?.order?.recipe == null)
        {
            Debug.LogError("There is no active Core order to submit.");
            return;
        }

        var ingredients = new List<PlacedIngredient>();
        if (activeOrder.order.recipe.layers == null)
        {
            Debug.LogError("The active recipe has no Core recipe layers.");
            return;
        }

        foreach (var layer in activeOrder.order.recipe.layers)
        {
            if (layer?.ingredient == null) continue;
            var quantity = quality == 2 ? layer.quantity : 1;
            for (var index = 0; index < quantity; index++)
            {
                ingredients.Add(new PlacedIngredient
                {
                    ingredient = layer.ingredient,
                    layerOrder = ingredients.Count,
                    cookState = CookState.NotRequired
                });
            }
        }

        if (quality == 0 && ingredients.Count > 0) ingredients.RemoveAt(ingredients.Count - 1);
        CounterSceneSession.SubmitCookedBurger(new BurgerData { placedIngredients = ingredients });
        SceneManager.LoadScene("Counter");
    }
}
