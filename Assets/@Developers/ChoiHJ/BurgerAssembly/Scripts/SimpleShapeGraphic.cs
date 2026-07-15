using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum SimpleShape
    {
        Rectangle,
        Circle,
        Triangle
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SimpleShapeGraphic : MaskableGraphic
    {
        [SerializeField] private SimpleShape shape = SimpleShape.Rectangle;
        [SerializeField, Range(8, 64)] private int circleSegments = 32;

        public SimpleShape Shape
        {
            get => shape;
            set
            {
                shape = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            switch (shape)
            {
                case SimpleShape.Circle:
                    PopulateCircle(vertexHelper);
                    break;
                case SimpleShape.Triangle:
                    PopulateTriangle(vertexHelper);
                    break;
                default:
                    PopulateRectangle(vertexHelper);
                    break;
            }
        }

        private void PopulateRectangle(VertexHelper vertexHelper)
        {
            Rect rect = GetPixelAdjustedRect();
            var vertices = new UIVertex[4];
            for (int index = 0; index < vertices.Length; index++)
            {
                vertices[index] = UIVertex.simpleVert;
                vertices[index].color = color;
            }

            vertices[0].position = new Vector2(rect.xMin, rect.yMin);
            vertices[1].position = new Vector2(rect.xMin, rect.yMax);
            vertices[2].position = new Vector2(rect.xMax, rect.yMax);
            vertices[3].position = new Vector2(rect.xMax, rect.yMin);
            vertexHelper.AddUIVertexQuad(vertices);
        }

        private void PopulateTriangle(VertexHelper vertexHelper)
        {
            Rect rect = GetPixelAdjustedRect();
            AddVertex(vertexHelper, new Vector2(rect.xMin, rect.yMin));
            AddVertex(vertexHelper, new Vector2(rect.center.x, rect.yMax));
            AddVertex(vertexHelper, new Vector2(rect.xMax, rect.yMin));
            vertexHelper.AddTriangle(0, 1, 2);
        }

        private void PopulateCircle(VertexHelper vertexHelper)
        {
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;

            AddVertex(vertexHelper, center);
            for (int index = 0; index < circleSegments; index++)
            {
                float radians = index * Mathf.PI * 2f / circleSegments;
                AddVertex(vertexHelper, center + new Vector2(Mathf.Cos(radians) * radiusX, Mathf.Sin(radians) * radiusY));
            }

            for (int index = 0; index < circleSegments; index++)
            {
                int next = (index + 1) % circleSegments;
                vertexHelper.AddTriangle(0, index + 1, next + 1);
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
        }
    }
}
