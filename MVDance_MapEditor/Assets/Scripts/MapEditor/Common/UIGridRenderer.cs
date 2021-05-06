using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class UIGridRenderer : Graphic
    {
        [Header("-- Base Config --")]
        [SerializeField] protected float thickness = 1f;
        public Vector2Int gridSize = new Vector2Int(1, 1);

        protected float cellWidth;
        protected float cellHeight;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            cellWidth = width / (float)gridSize.x;
            cellHeight = height / (float)gridSize.y;

            int count = 0;
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    DrawCell(x, y, count, vh);
                    count++;
                }
            }
        }

        protected virtual void DrawCell(int x, int y, int index, VertexHelper vh)
        {

        }
    }
}






