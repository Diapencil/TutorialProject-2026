using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// CookingScene 전체 UI와 완성/리셋 흐름을 관리합니다.
// 붙이는 오브젝트: CookingScene의 CookingUI 오브젝트.
public class CookingUI : MonoBehaviour
{
    private Canvas canvas;
    private BuildZone buildZone;
    private Button completeButton;
    private Button resetButton;
    private Image flashImage;
    private bool isFinishing;

    private void Start()
    {
        GameManager.EnsureExists().EnsureOrder();
        BuildInterface();
    }

    // 화면 전체 UI를 코드로 생성합니다.
    private void BuildInterface()
    {
        canvas = UIFactory.CreateCanvas("CookingCanvas");

        RectTransform background = UIFactory.CreateImage("Background", canvas.transform, new Color(0.11f, 0.12f, 0.13f), Vector2.zero);
        UIFactory.SetStretch(background);

        Text title = UIFactory.CreateText("Title", canvas.transform, "햄버거 조리대", 52, Color.white, TextAnchor.MiddleLeft);
        UIFactory.SetAnchor(title.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(300f, -70f), new Vector2(520f, 72f));

        Recipe order = GameManager.Instance.CurrentOrder;
        Text orderText = UIFactory.CreateText("OrderText", canvas.transform, "주문: " + order.DisplayName, 34, new Color(0.92f, 0.95f, 0.98f), TextAnchor.MiddleLeft);
        UIFactory.SetAnchor(orderText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(310f, -132f), new Vector2(560f, 54f));

        buildZone = CreateBuildZone();
        CreateTray();
        CreateButtons();
        CreateFlashOverlay();
    }

    private BuildZone CreateBuildZone()
    {
        RectTransform zone = UIFactory.CreateImage("BuildZone", canvas.transform, new Color(1f, 1f, 1f, 0.055f), new Vector2(560f, 380f));
        UIFactory.SetAnchor(zone, new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(560f, 380f));

        Outline outline = zone.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.28f);
        outline.effectDistance = new Vector2(3f, -3f);

        Text hint = UIFactory.CreateText("BuildZoneHint", zone, "Build Zone", 24, new Color(1f, 1f, 1f, 0.5f), TextAnchor.UpperCenter);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(0f, -56f);
        hintRect.offsetMax = new Vector2(0f, -12f);

        GameObject stackObject = new GameObject("BurgerStack", typeof(RectTransform));
        stackObject.transform.SetParent(zone, false);
        RectTransform stackRect = stackObject.GetComponent<RectTransform>();
        UIFactory.SetStretch(stackRect);

        BuildZone zoneComponent = zone.gameObject.AddComponent<BuildZone>();
        zoneComponent.Initialize(zone, stackRect, stackRect, canvas);
        return zoneComponent;
    }

    private void CreateTray()
    {
        RectTransform tray = UIFactory.CreateImage("IngredientTray", canvas.transform, new Color(0.19f, 0.20f, 0.21f), new Vector2(1060f, 160f));
        UIFactory.SetAnchor(tray, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(1060f, 160f));

        Outline outline = tray.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.13f);
        outline.effectDistance = new Vector2(2f, -2f);

        IngredientType[] trayIngredients = IngredientLibrary.TrayIngredients;
        float spacing = 132f;
        float startX = -spacing * (trayIngredients.Length - 1) * 0.5f;

        for (int i = 0; i < trayIngredients.Length; i++)
        {
            IngredientType type = trayIngredients[i];
            RectTransform icon = UIFactory.CreateIngredientIcon(type, tray, type.ToString() + "Icon", true);
            icon.anchoredPosition = new Vector2(startX + spacing * i, 0f);

            Button button = icon.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            IngredientDraggable draggable = icon.gameObject.AddComponent<IngredientDraggable>();
            draggable.Initialize(type, buildZone, canvas);
        }
    }

    private void CreateButtons()
    {
        completeButton = UIFactory.CreateButton("CompleteButton", canvas.transform, "완성", new Vector2(190f, 78f), new Color(0.13f, 0.53f, 0.32f));
        UIFactory.SetAnchor(completeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-130f, -78f), new Vector2(190f, 78f));
        completeButton.onClick.AddListener(OnCompleteClicked);

        resetButton = UIFactory.CreateButton("ResetButton", canvas.transform, "리셋", new Vector2(190f, 78f), new Color(0.42f, 0.23f, 0.24f));
        UIFactory.SetAnchor(resetButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-130f, -172f), new Vector2(190f, 78f));
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void CreateFlashOverlay()
    {
        RectTransform flash = UIFactory.CreateImage("ResultFlash", canvas.transform, Color.clear, Vector2.zero);
        UIFactory.SetStretch(flash);
        flash.SetAsLastSibling();
        flashImage = flash.GetComponent<Image>();
        flashImage.raycastTarget = false;
    }

    private void OnResetClicked()
    {
        if (isFinishing)
        {
            return;
        }

        buildZone.ResetStack();
    }

    private void OnCompleteClicked()
    {
        if (isFinishing)
        {
            return;
        }

        StartCoroutine(CompleteRoutine());
    }

    // 레시피를 판정하고 0.6초 뒤 CounterScene으로 돌아갑니다.
    private IEnumerator CompleteRoutine()
    {
        isFinishing = true;
        completeButton.interactable = false;
        resetButton.interactable = false;

        yield return buildZone.ShowBunTop();

        Recipe matchedRecipe = RecipeBook.FindMatchingRecipe(buildZone.GetIngredientsWithBaseBun());
        Recipe order = GameManager.Instance.CurrentOrder;
        bool success = matchedRecipe != null && matchedRecipe.Id == order.Id;

        if (success)
        {
            StartCoroutine(buildZone.BounceWholeStack());
            yield return FlashRoutine(new Color(0.1f, 0.85f, 0.34f, 1f));
            // TODO: 사운드 추가 - 성공 효과음
        }
        else
        {
            StartCoroutine(buildZone.ShakeWholeStack());
            yield return FlashRoutine(new Color(0.95f, 0.12f, 0.12f, 1f));
            // TODO: 사운드 추가 - 실패 효과음
        }

        GameManager.Instance.SaveCookingResult(success, matchedRecipe);
        yield return new WaitForSeconds(0.4f);
        KimSceneLoader.LoadCounterScene();
    }

    private IEnumerator FlashRoutine(Color color)
    {
        color.a = 0f;
        flashImage.color = color;

        float duration = 0.1f;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            Color c = color;
            c.a = Mathf.Lerp(0f, 0.3f, time / duration);
            flashImage.color = c;
            yield return null;
        }

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            Color c = color;
            c.a = Mathf.Lerp(0.3f, 0f, time / duration);
            flashImage.color = c;
            yield return null;
        }

        color.a = 0f;
        flashImage.color = color;
    }
}
