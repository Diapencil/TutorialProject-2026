using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lee.Counter
{
    [CreateAssetMenu(menuName = "Lee/Counter/Recipe Data", fileName = "Recipe_")]
    public sealed class RecipeData : ScriptableObject
    {
        [System.Serializable]
        public sealed class CustomerRequestDialogue
        {
            [Tooltip("처음 주문을 받을 때 손님이 하는 말입니다.")]
            [TextArea, SerializeField] private string initialRequest;

            [Tooltip("'뭐라고요?'를 눌렀을 때 손님이 덧붙여 하는 말입니다.")]
            [TextArea, SerializeField] private string clarificationRequest;

            public string InitialRequest => initialRequest;
            public string ClarificationRequest => clarificationRequest;

            public CustomerRequestDialogue()
            {
            }

            public CustomerRequestDialogue(string initialRequest, string clarificationRequest = "")
            {
                this.initialRequest = initialRequest;
                this.clarificationRequest = clarificationRequest;
            }
        }

        [SerializeField] private string recipeName;
        [Tooltip("주문이 들어올 때마다 이 목록에서 주문 대사와 재질문 대사를 한 쌍으로 무작위 선택합니다.")]
        [SerializeField] private List<CustomerRequestDialogue> customerRequestDialogues = new();

        // 기존 RecipeData 에셋의 customerRequests 값을 보존해 새 구조로 자동 이전합니다.
        [FormerlySerializedAs("customerRequests")]
        [HideInInspector, SerializeField] private List<string> legacyCustomerRequests = new();
        [Min(0), SerializeField] private int baseReward = 100;
        [SerializeField] private List<IngredientType> ingredients = new();

        public string RecipeName => recipeName;
        public int BaseReward => baseReward;
        public IReadOnlyList<IngredientType> Ingredients => ingredients;

        public string GetRandomCustomerRequest()
        {
            return GetRandomCustomerRequestDialogue().InitialRequest;
        }

        /// <summary>
        /// 한 주문에 대응하는 첫 대사와 재질문 대사를 함께 반환합니다.
        /// 선택된 이 값을 주문 데이터에 보관하면 '뭐라고요?'에도 같은 손님의 대사를 표시할 수 있습니다.
        /// </summary>
        public CustomerRequestDialogue GetRandomCustomerRequestDialogue()
        {
            MigrateLegacyCustomerRequests();

            if (customerRequestDialogues == null || customerRequestDialogues.Count == 0)
            {
                Debug.LogWarning($"'{name}' 레시피에 주문 대사가 없습니다. 레시피 이름을 대신 표시합니다.", this);
                return new CustomerRequestDialogue($"{recipeName} 하나 주세요!");
            }

            return customerRequestDialogues[Random.Range(0, customerRequestDialogues.Count)];
        }

        private void OnValidate()
        {
            MigrateLegacyCustomerRequests();
        }

        private void MigrateLegacyCustomerRequests()
        {
            if ((customerRequestDialogues != null && customerRequestDialogues.Count > 0) ||
                legacyCustomerRequests == null || legacyCustomerRequests.Count == 0)
            {
                return;
            }

            customerRequestDialogues = new List<CustomerRequestDialogue>(legacyCustomerRequests.Count);
            foreach (var request in legacyCustomerRequests)
            {
                customerRequestDialogues.Add(new CustomerRequestDialogue(request));
            }
        }
    }
}
