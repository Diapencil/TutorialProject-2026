using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 빵 위에 재료를 쌓는 영역과 스택 리스트를 관리합니다.
// 붙이는 오브젝트: CookingScene의 BuildZone 오브젝트.
public class BuildZone : MonoBehaviour
{
    private static readonly Vector2 StackLayerSize = new Vector2(300f, 52f);
    private static readonly Vector2 BaseBunSize = new Vector2(320f, 62f);

    private readonly List<IngredientType> currentStack = new List<IngredientType>();
    private readonly List<RectTransform> stackVisuals = new List<RectTransform>();

    private RectTransform zoneRect;
    private RectTransform stackParent;
    private RectTransform effectsParent;
    private RectTransform baseBunVisual;
    private RectTransform bunTopVisual;

    public IReadOnlyList<IngredientType> CurrentStack => currentStack;

    public void Initialize(RectTransform targetZoneRect, RectTransform targetStackParent, RectTransform targetEffectsParent, Canvas targetCanvas)
    {
        zoneRect = targetZoneRect;
        stackParent = targetStackParent;
        effectsParent = targetEffectsParent;
        CreateBaseBun();
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
    {
        return zoneRect != null && RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPoint, eventCamera);
    }

    // 드롭된 재료를 스택 리스트에 추가하고 스냅/바운스 애니메이션을 시작합니다.
    public void AcceptIngredient(IngredientType type, RectTransform visual)
    {
        if (visual == null || stackParent == null)
        {
            return;
        }

        RemoveDragShadow(visual);
        visual.SetParent(stackParent, true);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.sizeDelta = StackLayerSize;
        StretchAcceptedCard(visual);
        visual.SetAsLastSibling();

        currentStack.Add(type);
        stackVisuals.Add(visual);

        int layerIndex = currentStack.Count - 1;
        Vector2 targetPosition = GetIngredientPosition(layerIndex);

        StartCoroutine(PlaceIngredientRoutine(visual, targetPosition));
        ReactToNewWeight(visual);
        SpawnLandingParticles(targetPosition);
        // TODO: 사운드 추가 - 재료 착지 효과음
    }

    // 판정용 재료 리스트입니다. 하단 빵은 항상 포함하고 윗빵은 장식이라 제외합니다.
    public List<IngredientType> GetIngredientsWithBaseBun()
    {
        List<IngredientType> ingredients = new List<IngredientType> { IngredientType.Bun };
        ingredients.AddRange(currentStack);
        return ingredients;
    }

    // 완성 버튼을 누를 때 윗빵을 자동으로 얹습니다.
    public IEnumerator ShowBunTop()
    {
        if (bunTopVisual != null)
        {
            yield break;
        }

        IngredientDefinition definition = IngredientLibrary.Get(IngredientType.BunTop);
        bunTopVisual = UIFactory.CreateImage("BunTop", stackParent, definition.Color, BaseBunSize);
        bunTopVisual.SetAsLastSibling();

        Text label = UIFactory.CreateText("Label", bunTopVisual, definition.DisplayName, 20, new Color(0.16f, 0.10f, 0.04f), TextAnchor.MiddleCenter);
        UIFactory.StretchToParent(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Vector2 target = GetBunTopPosition();
        bunTopVisual.anchoredPosition = target + new Vector2(0f, 70f);
        bunTopVisual.localScale = new Vector3(1.05f, 0.85f, 1f);

        yield return SimpleTween.MoveTo(bunTopVisual, target, 0.15f, SimpleTween.EaseOutQuad);
        yield return SquashRoutine(bunTopVisual);
    }

    // 리셋: 쌓인 재료를 위로 튕겨 날려 보내고 리스트를 비웁니다.
    public void ResetStack()
    {
        for (int i = 0; i < stackVisuals.Count; i++)
        {
            RectTransform visual = stackVisuals[i];
            if (visual != null)
            {
                StartCoroutine(FlyAwayRoutine(visual));
            }
        }

        stackVisuals.Clear();
        currentStack.Clear();

        if (bunTopVisual != null)
        {
            StartCoroutine(FlyAwayRoutine(bunTopVisual));
            bunTopVisual = null;
        }

        // TODO: 사운드 추가 - 리셋 효과음
    }

    public IEnumerator BounceWholeStack()
    {
        if (stackParent == null)
        {
            yield break;
        }

        yield return SimpleTween.ScaleTo(stackParent, Vector3.one * 1.15f, 0.11f, SimpleTween.EaseOutQuad);
        yield return SimpleTween.ScaleTo(stackParent, Vector3.one, 0.13f, SimpleTween.EaseOutBack);
    }

    public IEnumerator ShakeWholeStack()
    {
        if (stackParent == null)
        {
            yield break;
        }

        yield return SimpleTween.Shake(stackParent, 0.28f, 14f);
    }

    private void CreateBaseBun()
    {
        if (baseBunVisual != null)
        {
            return;
        }

        IngredientDefinition definition = IngredientLibrary.Get(IngredientType.Bun);
        baseBunVisual = UIFactory.CreateImage("BaseBun", stackParent, definition.Color, BaseBunSize);
        baseBunVisual.anchoredPosition = GetBaseBunPosition();
        baseBunVisual.SetAsFirstSibling();

        Text label = UIFactory.CreateText("Label", baseBunVisual, definition.DisplayName, 20, new Color(0.16f, 0.10f, 0.04f), TextAnchor.MiddleCenter);
        UIFactory.StretchToParent(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
    }

    private IEnumerator PlaceIngredientRoutine(RectTransform visual, Vector2 targetPosition)
    {
        yield return SimpleTween.RotateZTo(visual, 0f, 0.1f, SimpleTween.EaseOutQuad);
        yield return SimpleTween.MoveTo(visual, targetPosition, 0.15f, SimpleTween.EaseOutQuad);
        yield return SquashRoutine(visual);
    }

    private IEnumerator SquashRoutine(RectTransform visual)
    {
        yield return SimpleTween.ScaleTo(visual, new Vector3(1.2f, 0.8f, 1f), 0.055f, SimpleTween.EaseOutQuad);
        yield return SimpleTween.ScaleTo(visual, new Vector3(0.9f, 1.1f, 1f), 0.075f, SimpleTween.EaseOutQuad);
        yield return SimpleTween.ScaleTo(visual, Vector3.one, 0.07f, SimpleTween.EaseOutQuad);
    }

    private void ReactToNewWeight(RectTransform newVisual)
    {
        for (int i = 0; i < stackVisuals.Count; i++)
        {
            RectTransform visual = stackVisuals[i];
            if (visual != null && visual != newVisual)
            {
                StartCoroutine(SimpleTween.PunchScale(visual, 1.03f, 0.16f));
            }
        }

        if (baseBunVisual != null)
        {
            StartCoroutine(SimpleTween.PunchScale(baseBunVisual, 1.03f, 0.16f));
        }
    }

    private void SpawnLandingParticles(Vector2 localPosition)
    {
        for (int i = 0; i < 4; i++)
        {
            RectTransform particle = UIFactory.CreateImage("LandingDot", effectsParent, Color.white, new Vector2(16f, 16f), UIFactory.CircleSprite);
            particle.anchoredPosition = localPosition + new Vector2(Random.Range(-20f, 20f), Random.Range(-4f, 18f));
            particle.GetComponent<Image>().raycastTarget = false;

            CanvasGroup group = particle.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.95f;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 target = particle.anchoredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(34f, 64f);
            StartCoroutine(ParticleRoutine(particle, group, target));
        }
    }

    private IEnumerator ParticleRoutine(RectTransform particle, CanvasGroup group, Vector2 target)
    {
        StartCoroutine(SimpleTween.FadeCanvasGroup(group, 0f, 0.24f, SimpleTween.EaseOutQuad));
        StartCoroutine(SimpleTween.ScaleTo(particle, Vector3.one * 1.8f, 0.24f, SimpleTween.EaseOutQuad));
        yield return SimpleTween.MoveTo(particle, target, 0.24f, SimpleTween.EaseOutQuad);

        if (particle != null)
        {
            Destroy(particle.gameObject);
        }
    }

    private IEnumerator FlyAwayRoutine(RectTransform visual)
    {
        if (visual == null)
        {
            yield break;
        }

        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = visual.gameObject.AddComponent<CanvasGroup>();
        }

        Vector2 start = visual.anchoredPosition;
        Vector2 end = start + new Vector2(Random.Range(-160f, 160f), Random.Range(260f, 420f));
        StartCoroutine(SimpleTween.ScaleTo(visual, Vector3.one * 0.25f, 0.3f, SimpleTween.EaseInQuad));
        StartCoroutine(SimpleTween.FadeCanvasGroup(group, 0f, 0.3f, SimpleTween.EaseInQuad));
        yield return SimpleTween.MoveTo(visual, end, 0.3f, SimpleTween.EaseOutQuad);

        if (visual != null)
        {
            Destroy(visual.gameObject);
        }
    }

    private void RemoveDragShadow(RectTransform visual)
    {
        Transform shadow = visual.Find("DragShadow");
        if (shadow != null)
        {
            Destroy(shadow.gameObject);
        }
    }

    private void StretchAcceptedCard(RectTransform visual)
    {
        RectTransform card = visual.Find("Card") as RectTransform;
        if (card != null)
        {
            UIFactory.StretchToParent(card, Vector2.zero, Vector2.zero);
        }
    }

    private Vector2 GetBaseBunPosition()
    {
        return new Vector2(0f, -76f);
    }

    private Vector2 GetIngredientPosition(int layerIndex)
    {
        return new Vector2(0f, -48f + layerIndex * 12f);
    }

    private Vector2 GetBunTopPosition()
    {
        return new Vector2(0f, -28f + currentStack.Count * 12f);
    }
}
