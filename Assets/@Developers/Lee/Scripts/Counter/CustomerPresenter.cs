using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Lee.Counter
{
    /// <summary>손님 프리팹의 표시/애니메이션 어댑터입니다. Animator는 선택 사항입니다.</summary>
    public sealed class CustomerPresenter : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Animator animator;
        [SerializeField] private string enterTrigger = "Enter";
        [SerializeField] private string exitTrigger = "Exit";
        [SerializeField, Min(0f)] private float enterDuration = 0.25f;
        [SerializeField, Min(0f)] private float exitDuration = 0.2f;
        [SerializeField] private AnimationCurve enterScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.75f, 1.1f),
            new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve exitScaleCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.25f, 1.1f),
            new Keyframe(1f, 0f));

        private Vector3 visibleScale;
        private Coroutine scaleRoutine;

        public float EnterDuration => enterDuration;

        public void SetSprite(Sprite sprite)
        {
            if (sprite == null) return;
            image.sprite = sprite;
        }

        private void Awake()
        {
            // 프리팹에 저장된 스케일을 손님이 완전히 나타났을 때의 크기로 사용한다.
            visibleScale = transform.localScale;
        }

        public void Enter()
        {
            if (animator != null) animator.SetTrigger(enterTrigger);
            PlayScale(enterScaleCurve, enterDuration);
        }

        public void Exit()
        {
            if (animator != null) animator.SetTrigger(exitTrigger);
            PlayScale(exitScaleCurve, exitDuration);
        }

        /// <summary>애니메이션 없이 즉시 숨긴다. 다음 손님을 맞기 전 초기 상태로 되돌릴 때 사용한다.</summary>
        public void Hide()
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            SetScaleRatio(0f);
        }

        /// <summary>애니메이션 없이 즉시 보여준다. Cooking 씬에서 돌아왔을 때처럼 이미 서 있는 상태를 표현할 때 사용한다.</summary>
        public void ShowImmediate()
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            SetScaleRatio(1f);
        }

        private void PlayScale(AnimationCurve curve, float duration)
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleY(curve, duration));
        }

        private IEnumerator ScaleY(AnimationCurve curve, float duration)
        {
            if (duration <= 0f)
            {
                SetScaleRatio(curve.Evaluate(1f));
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetScaleRatio(curve.Evaluate(elapsed / duration));
                yield return null;
            }

            SetScaleRatio(curve.Evaluate(1f));
            scaleRoutine = null;
        }

        private void SetScaleRatio(float ratio)
        {
            transform.localScale = new Vector3(
                visibleScale.x,
                visibleScale.y * ratio,
                visibleScale.z);
        }
    }
}
