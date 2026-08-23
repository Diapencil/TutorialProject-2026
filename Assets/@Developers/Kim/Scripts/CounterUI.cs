using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// CounterScene의 주문 표시, 결과 표시, 씬 전환을 관리합니다.
// 붙이는 오브젝트: CounterScene의 CounterUI 오브젝트.
public class CounterUI : MonoBehaviour
{
    private Text orderText;
    private Text resultText;

    private void Start()
    {
        GameManager.EnsureExists().EnsureOrder();
        BuildInterface();
        RefreshTexts();
    }

    // 카운터 화면 UI를 코드로 생성합니다.
    private void BuildInterface()
    {
        Canvas canvas = UIFactory.CreateCanvas("CounterCanvas");

        RectTransform background = UIFactory.CreateImage("Background", canvas.transform, new Color(0.12f, 0.11f, 0.10f), Vector2.zero);
        UIFactory.SetStretch(background);

        RectTransform counterBand = UIFactory.CreateImage("CounterBand", canvas.transform, new Color(0.25f, 0.23f, 0.20f), new Vector2(1920f, 210f));
        counterBand.anchorMin = new Vector2(0.5f, 0f);
        counterBand.anchorMax = new Vector2(0.5f, 0f);
        counterBand.pivot = new Vector2(0.5f, 0f);
        counterBand.anchoredPosition = Vector2.zero;

        Text title = UIFactory.CreateText("Title", canvas.transform, "카운터 씬", 58, Color.white, TextAnchor.MiddleCenter);
        UIFactory.SetAnchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(680f, 90f));

        orderText = UIFactory.CreateText("OrderText", canvas.transform, string.Empty, 44, new Color(0.97f, 0.94f, 0.88f), TextAnchor.MiddleCenter);
        UIFactory.SetAnchor(orderText.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(920f, 90f));

        resultText = UIFactory.CreateText("ResultText", canvas.transform, string.Empty, 34, new Color(0.80f, 0.88f, 0.94f), TextAnchor.MiddleCenter);
        UIFactory.SetAnchor(resultText.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(980f, 110f));

        Button cookButton = UIFactory.CreateButton("CookButton", canvas.transform, "요리하러 가기", new Vector2(290f, 84f), new Color(0.15f, 0.42f, 0.58f));
        UIFactory.SetAnchor(cookButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-170f, -110f), new Vector2(290f, 84f));
        cookButton.onClick.AddListener(GoToCooking);

        Button nextButton = UIFactory.CreateButton("NextButton", canvas.transform, "다음 손님", new Vector2(250f, 84f), new Color(0.39f, 0.33f, 0.25f));
        UIFactory.SetAnchor(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(170f, -110f), new Vector2(250f, 84f));
        nextButton.onClick.AddListener(PickNextCustomer);
    }

    private void RefreshTexts()
    {
        Recipe order = GameManager.Instance.CurrentOrder;
        orderText.text = "손님 주문: " + order.DisplayName;
        resultText.text = GameManager.Instance.HasLastResult
            ? GameManager.Instance.BuildResultMessage()
            : "주문을 확인하고 조리대로 이동하세요.";
    }

    private void GoToCooking()
    {
        GameManager.Instance.EnsureOrder();
        KimSceneLoader.LoadCookingScene();
    }

    private void PickNextCustomer()
    {
        GameManager.Instance.PickNextOrder();
        RefreshTexts();
    }
}
