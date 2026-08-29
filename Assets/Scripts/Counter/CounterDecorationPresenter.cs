// 카운터 씬에서 구매한 장식 오브젝트만 표시한다.
using System;
using System.Collections.Generic;
using SheepSheepBurger.Core;
using UnityEngine;

namespace SheepSheepBurger.Counter
{
    [DisallowMultipleComponent]
    public sealed class CounterDecorationPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class DecorationVisual
        {
            public int decorationId;
            public GameObject visual;
        }

        [SerializeField] private List<DecorationVisual> decorationVisuals = new List<DecorationVisual>();

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        [ContextMenu("Refresh Purchased Decorations")]
        public void Refresh()
        {
            GameState state = GameManager.GetOrCreate().State;

            for (int i = 0; i < decorationVisuals.Count; i++)
            {
                DecorationVisual entry = decorationVisuals[i];
                if (entry?.visual != null)
                {
                    entry.visual.SetActive(state.IsDecorationPurchased(entry.decorationId));
                }
            }
        }
    }
}
