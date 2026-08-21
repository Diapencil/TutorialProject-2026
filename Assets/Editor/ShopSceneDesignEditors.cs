using SheepSheepBurger.Shop;
using UnityEditor;
using UnityEngine;

namespace SheepSheepBurger.EditorTools
{
    [CustomEditor(typeof(ShopSceneDesignPreset))]
    public sealed class ShopSceneDesignPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("열린 상점 씬에 디자인 적용"))
            {
                ((ShopSceneDesignPreset)target).ApplyToOpenScenes();
            }
        }
    }

    [CustomEditor(typeof(ShopSceneDesignController))]
    public sealed class ShopSceneDesignControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("디자인 프리셋 적용"))
            {
                ((ShopSceneDesignController)target).ApplyDesign();
            }
        }
    }

    [CustomEditor(typeof(ShopSlotDesignPresenter))]
    public sealed class ShopSlotDesignPresenterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("슬롯 디자인 적용"))
            {
                ((ShopSlotDesignPresenter)target).ApplyDesign();
            }
        }
    }
}
