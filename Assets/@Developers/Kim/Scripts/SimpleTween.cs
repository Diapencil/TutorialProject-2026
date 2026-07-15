using System.Collections;
using UnityEngine;

// 외부 tween 패키지 없이 UI 애니메이션을 재사용하기 위한 작은 보간 유틸리티입니다.
// 붙이는 오브젝트: 없음. MonoBehaviour에서 StartCoroutine(SimpleTween....) 형태로 사용합니다.
public static class SimpleTween
{
    public delegate float Ease(float t);

    public static IEnumerator MoveTo(RectTransform target, Vector2 end, float duration, Ease ease = null)
    {
        if (target == null)
        {
            yield break;
        }

        Vector2 start = target.anchoredPosition;
        yield return ValueOverTime(duration, ease, t =>
        {
            if (target != null)
            {
                target.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
            }
        });
    }

    public static IEnumerator ScaleTo(RectTransform target, Vector3 end, float duration, Ease ease = null)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 start = target.localScale;
        yield return ValueOverTime(duration, ease, t =>
        {
            if (target != null)
            {
                target.localScale = Vector3.LerpUnclamped(start, end, t);
            }
        });
    }

    public static IEnumerator RotateZTo(RectTransform target, float endZ, float duration, Ease ease = null)
    {
        if (target == null)
        {
            yield break;
        }

        float startZ = target.localEulerAngles.z;
        if (startZ > 180f)
        {
            startZ -= 360f;
        }

        yield return ValueOverTime(duration, ease, t =>
        {
            if (target != null)
            {
                float z = Mathf.LerpUnclamped(startZ, endZ, t);
                target.localRotation = Quaternion.Euler(0f, 0f, z);
            }
        });
    }

    public static IEnumerator FadeCanvasGroup(CanvasGroup group, float end, float duration, Ease ease = null)
    {
        if (group == null)
        {
            yield break;
        }

        float start = group.alpha;
        yield return ValueOverTime(duration, ease, t =>
        {
            if (group != null)
            {
                group.alpha = Mathf.LerpUnclamped(start, end, t);
            }
        });
    }

    // 짧은 좌우 흔들림 피드백입니다.
    public static IEnumerator Shake(RectTransform target, float duration, float amplitude)
    {
        if (target == null)
        {
            yield break;
        }

        Vector2 origin = target.anchoredPosition;
        float time = 0f;

        while (time < duration)
        {
            if (target == null)
            {
                yield break;
            }

            float progress = time / duration;
            float strength = 1f - progress;
            float x = Mathf.Sin(progress * Mathf.PI * 8f) * amplitude * strength;
            target.anchoredPosition = origin + new Vector2(x, 0f);
            time += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            target.anchoredPosition = origin;
        }
    }

    // 아래 재료가 살짝 눌리는 듯한 짧은 반응입니다.
    public static IEnumerator PunchScale(RectTransform target, float peakScale, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 original = target.localScale;
        yield return ScaleTo(target, original * peakScale, duration * 0.45f, EaseOutQuad);
        yield return ScaleTo(target, original, duration * 0.55f, EaseOutQuad);
    }

    private static IEnumerator ValueOverTime(float duration, Ease ease, System.Action<float> apply)
    {
        if (duration <= 0f)
        {
            apply(1f);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            float t = Mathf.Clamp01(time / duration);
            apply(ease != null ? ease(t) : t);
            time += Time.deltaTime;
            yield return null;
        }

        apply(1f);
    }

    public static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseInQuad(float t)
    {
        return t * t;
    }
}
