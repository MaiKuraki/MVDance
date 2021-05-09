using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class SubTimelineGrid : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] Transform grid_root;
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


        public double GetGridOriginWidth() => gridOriginWidth;

        void Awake()
        {
            grid_rt = grid_root.GetComponent<RectTransform>();
            grid_value_root_rt = grid_value_root.GetComponent<RectTransform>();

            gridOriginWidth = grid_rt.rect.width;

            visible_main_grid_amount = max_main_grid_amount;
            grid_main.gridSize = new Vector2Int(max_main_grid_amount, 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);
            grid_sub.gridSize = new Vector2Int(max_main_grid_amount * 10, 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);
        }

        public void AddGridXPosition(float deltaOffsetX)
        {
            grid_root.localPosition = new Vector2(grid_root.localPosition.x + deltaOffsetX, grid_root.localPosition.y);
        }

        public void SetGridXPosition(float newPosX)
        {
            grid_root.localPosition = new Vector2(newPosX, grid_root.localPosition.y);
        }

        public void AddGridValueXPosition(float deltaOffsetX)
        {
            grid_value_root.localPosition = new Vector2(grid_value_root.localPosition.x + deltaOffsetX, grid_value_root.localPosition.y);
        }

        public void SetGridValueXPosition(float newPosX)
        {
            //  TODO 
            //  don't know why zero doesn't work
            grid_value_root.localPosition = new Vector2(newPosX, grid_value_root.localPosition.y);
        }

        public void RefreshGrid(int newVidibleGridAmount, long startValue)
        {
            /** -- refresh render -- */
            double ratio = max_main_grid_amount / (double)newVidibleGridAmount;
            double full_timeline_width = gridOriginWidth * ratio;
            // print($"ratio{ratio}, targetWidth: {final_grid_width}");
            grid_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_rt.rect.height);

            /** -- refresh number value -- */
            grid_value_root_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_value_root_rt.rect.height);
            for (int i = 0; i < grid_value_root.childCount; i++)
            {
                Destroy(grid_value_root.GetChild(i).gameObject);
            }

            for (int i = 0; i < max_main_grid_amount + 1; i++)
            {
                Transform t = Instantiate(grid_value_text_tr);
                t.GetComponent<Text>().text = (i + startValue).ToString();
                t.SetParent(grid_value_root);
                t.localScale = Vector3.one;
                float posX = i * grid_value_root_rt.rect.width / max_main_grid_amount;
                t.localPosition = new Vector2(posX, -10);
            }
        }
    }
}

