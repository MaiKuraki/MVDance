using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class SubTimelineGrid : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] Transform grid_root;
        [SerializeField] Transform gird_visual;
        [SerializeField] UIGridRenderer grid_main;
        [SerializeField] UIGridRenderer grid_sub;
        [SerializeField] Transform grid_value_root;
        [SerializeField] Transform grid_value_text_tr;

        int max_main_grid_amount = 21;
        int min_main_grid_amount = 1;
        int visible_main_grid_amount;

        double gridOriginWidth;
        RectTransform grid_rt;
        RectTransform grid_value_root_rt;

        double cachedOffsetX = 0;


        public double GetGridOriginWidth() => gridOriginWidth;
        public double GetGridWidth() => grid_rt.rect.width;
        void Awake()
        {
            grid_rt = grid_root.GetComponent<RectTransform>();
            grid_value_root_rt = grid_value_root.GetComponent<RectTransform>();
            //  there is one more grid hidden out of screen, grid origin width is the visual width
            gridOriginWidth = grid_rt.rect.width;

            visible_main_grid_amount = max_main_grid_amount;
            grid_main.gridSize = new Vector2Int(max_main_grid_amount, 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);
            grid_sub.gridSize = new Vector2Int(max_main_grid_amount * 10, 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);

            
            for (int i = 0; i < grid_value_root.childCount; i++)
            {
                Destroy(grid_value_root.GetChild(i).gameObject);
            }
            for (int i = 0; i < max_main_grid_amount + 1; i++)
            {
                Transform t = Instantiate(grid_value_text_tr);
                t.SetParent(grid_value_root);
                t.localScale = Vector3.one;
                float posX = i * grid_value_root_rt.rect.width / (max_main_grid_amount - 1);
                t.localPosition = new Vector3(posX, -10 , 0);
            }
        }

        public void SetGridXPosition(float newPosX)
        {
            grid_root.localPosition = new Vector2(newPosX, grid_root.localPosition.y);
        }

        public void SetGridValueXPosition(float newOffsetPosition)
        {
            for (int i = 0; i < max_main_grid_amount + 1; i++)
            {
                Transform t = grid_value_root.GetChild(i);
                float posX = i * grid_value_root_rt.rect.width / max_main_grid_amount + newOffsetPosition;
                t.localPosition = new Vector2(posX, -10);
            }

            cachedOffsetX = newOffsetPosition;
        }

        public void UpdateGridValueTextVal(long startVal)
        {
            for (int i = 0; i < max_main_grid_amount + 1; i++)
            {
                Transform t = grid_value_root.GetChild(i);
                t.GetComponent<Text>().text = (i + startVal).ToString();
            }
        }

        public void RefreshGrid(int newVidibleGridAmount, long startVal)
        {
            /** -- refresh render -- */
            double new_grid_ratio = max_main_grid_amount / (double)newVidibleGridAmount;
            double full_timeline_width = gridOriginWidth * new_grid_ratio;

            grid_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_rt.rect.height);
            grid_value_root_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_value_root_rt.rect.height);

            /** -- refresh number value -- */
            SetGridValueXPosition(0);
            UpdateGridValueTextVal(startVal);
        }
    }
}

