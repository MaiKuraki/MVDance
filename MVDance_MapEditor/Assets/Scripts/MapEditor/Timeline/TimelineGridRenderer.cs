using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class TimelineGridRenderer : UIGridRenderer
    {
        [Header("-- Timeline Config --")]
        public bool draw_top = true;
        public bool draw_bottom = true;
        public bool draw_left = true;
        public bool draw_right = true;
        protected override void DrawCell(int x, int y, int index, VertexHelper vh)
        {
            base.DrawCell(x, y, index, vh);

            float xPos = cellWidth * x;
            float yPos = cellHeight * y;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(xPos, yPos);
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos, yPos + cellHeight);
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos + cellWidth, yPos + cellHeight);
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos + cellWidth, yPos);
            vh.AddVert(vertex);

            float widthSqr = thickness * thickness;
            float distanceSqr = widthSqr / 2f;
            float distance = Mathf.Sqrt(distanceSqr);

            vertex.position = new Vector3(xPos + distance, yPos + distance);
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos + distance, yPos + (cellHeight - distance));
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos + (cellWidth - distance), yPos + (cellHeight - distance));
            vh.AddVert(vertex);

            vertex.position = new Vector3(xPos + (cellWidth - distance), yPos + distance);
            vh.AddVert(vertex);

            int offset = index * 8;

            //  left
            if (draw_left)
            {
                vh.AddTriangle(offset + 0, offset + 1, offset + 5);
                vh.AddTriangle(offset + 5, offset + 4, offset + 0);
            }


            //  top
            if (draw_top)
            {
                vh.AddTriangle(offset + 1, offset + 2, offset + 6);
                vh.AddTriangle(offset + 6, offset + 5, offset + 1);
            }

            //  right
            if (draw_right)
            {
                vh.AddTriangle(offset + 2, offset + 3, offset + 7);
                vh.AddTriangle(offset + 7, offset + 6, offset + 2);
            }

            //  bottom
            if (draw_bottom)
            {
                vh.AddTriangle(offset + 3, offset + 0, offset + 4);
                vh.AddTriangle(offset + 4, offset + 7, offset + 3);
            }
        }
    }
}


