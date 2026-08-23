using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum SimpleShape
    {
        Rectangle,
        Circle,
        Triangle,
        RoundedRectangle
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SimpleShapeGraphic : Image
    {
        [SerializeField] private SimpleShape shape = SimpleShape.Rectangle;
        [SerializeField] private Sprite sourceSprite;
        [SerializeField, Min(0f)] private float cornerRadius = 24f;

        public SimpleShape Shape
        {
            get => shape;
            set
            {
                shape = value;
                ApplySprite();
            }
        }

        public Sprite SourceSprite
        {
            get => sourceSprite;
            set
            {
                sourceSprite = value;
                ApplySprite();
            }
        }

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                ApplySprite();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplySprite();
        }

        private void ApplySprite()
        {
            BurgerSpriteCatalog catalog = BurgerSpriteCatalog.Active;
            sprite = sourceSprite != null
                ? sourceSprite
                : catalog != null
                    ? catalog.GetShape(shape)
                    : null;
            type = Type.Simple;
            preserveAspect = sourceSprite != null;
            SetAllDirty();
        }
    }
}
