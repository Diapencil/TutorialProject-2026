using UnityEngine;

namespace Lee.Counter
{
    /// <summary>손님 프리팹의 표시/애니메이션 어댑터입니다. Animator는 선택 사항입니다.</summary>
    public sealed class CustomerPresenter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string enterTrigger = "Enter";
        [SerializeField] private string exitTrigger = "Exit";

        public void Enter() { if (animator != null) animator.SetTrigger(enterTrigger); }
        public void Exit() { if (animator != null) animator.SetTrigger(exitTrigger); }
    }
}
