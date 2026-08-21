using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SauceStrokeGraphic : MaskableGraphic
    {
        private readonly List<Vector2> points = new List<Vector2>();

        [SerializeField] private IngredientType sauceType;
        [SerializeField] private Sprite sourceSprite;
        [SerializeField, Min(1f)] private float diameter = 22f;
        [SerializeField] private int layerOrder;

        public IngredientType SauceType => sauceType;

        public Sprite SourceSprite => sourceSprite;

        public float Diameter => diameter;

        public int LayerOrder => layerOrder;

        public int PointCount => points.Count;

        public IReadOnlyList<Vector2> Points => points;

        public override Texture mainTexture =>
            sourceSprite != null && sourceSprite.texture != null
                ? sourceSprite.texture
                : s_WhiteTexture;

        public void Configure(
            IngredientType type,
            Sprite sprite,
            Color tint,
            float brushDiameter,
            int order)
        {
            sauceType = type;
            sourceSprite = sprite;
            color = tint;
            diameter = Mathf.Max(1f, brushDiameter);
            layerOrder = order;
            raycastTarget = false;
            SetAllDirty();
        }

        public void AddPoint(Vector2 point)
        {
            points.Add(point);
            SetVerticesDirty();
        }

        public void SetPoints(IEnumerable<Vector2> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            points.Clear();
            points.AddRange(source);
            SetVerticesDirty();
        }

        public List<Vector2> ExtractPoints(Predicate<Vector2> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var extracted = new List<Vector2>();
            for (int index = points.Count - 1; index >= 0; index--)
            {
                Vector2 point = points[index];
                if (!predicate(point))
                {
                    continue;
                }

                extracted.Add(point);
                points.RemoveAt(index);
            }

            extracted.Reverse();
            if (extracted.Count > 0)
            {
                SetVerticesDirty();
            }
            return extracted;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (points.Count == 0)
            {
                return;
            }

            Vector4 uv = sourceSprite != null
                ? DataUtility.GetOuterUV(sourceSprite)
                : new Vector4(0f, 0f, 1f, 1f);
            float radius = diameter * 0.5f;
            Color32 vertexColor = color;

            for (int index = 0; index < points.Count; index++)
            {
                Vector2 point = points[index];
                int vertexStart = vertexHelper.currentVertCount;
                vertexHelper.AddVert(
                    new Vector3(point.x - radius, point.y - radius, 0f),
                    vertexColor,
                    new Vector2(uv.x, uv.y));
                vertexHelper.AddVert(
                    new Vector3(point.x - radius, point.y + radius, 0f),
                    vertexColor,
                    new Vector2(uv.x, uv.w));
                vertexHelper.AddVert(
                    new Vector3(point.x + radius, point.y + radius, 0f),
                    vertexColor,
                    new Vector2(uv.z, uv.w));
                vertexHelper.AddVert(
                    new Vector3(point.x + radius, point.y - radius, 0f),
                    vertexColor,
                    new Vector2(uv.z, uv.y));
                vertexHelper.AddTriangle(vertexStart, vertexStart + 1, vertexStart + 2);
                vertexHelper.AddTriangle(vertexStart, vertexStart + 2, vertexStart + 3);
            }
        }
    }
}
