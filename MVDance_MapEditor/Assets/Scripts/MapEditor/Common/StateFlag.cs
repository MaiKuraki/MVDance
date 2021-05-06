using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class SignalStateSetting
    {
        public string state_text = "--NULL--";
        public Color flag_color = Color.gray;
        public Color full_bg_color = Color.gray;
        public Color value_bg_color = Color.gray;
    }

    public class StateFlag : MonoBehaviour
    {
        [Header("-- Element --")]
        [SerializeField] Image fullbg;
        [SerializeField] Image flag;
        [SerializeField] Text title;
        [SerializeField] Text value;
        [SerializeField] Image value_bg;
        [SerializeField] Text copy_vale;
        [Header("-- Setting --")]
        [SerializeField] string titleStr;
        [SerializeField] bool isUsingValueBG = false;
        [SerializeField] bool isUsingFullBG = false;

        private void Awake()
        {
            title.text = !string.IsNullOrEmpty(titleStr) ? titleStr : "--NULL--";
            value.text = "NULL";
            flag.color = Color.white;
            copy_vale.text = value.text;
            fullbg.gameObject.SetActive(isUsingFullBG);
            copy_vale.gameObject.SetActive(isUsingValueBG);
            value_bg.gameObject.SetActive(isUsingValueBG);
        }

        public void UpdateState(SignalStateSetting setting, bool newUsingValueBg = false, bool newUsingFullBg = false)
        {
            value.text = setting.state_text;
            flag.color = setting.flag_color;

            isUsingValueBG = newUsingValueBg;
            if (isUsingValueBG)
            {
                copy_vale.gameObject.SetActive(true);
                copy_vale.text = value.text;
                value_bg.gameObject.SetActive(true);
                value_bg.color = setting.value_bg_color;
            }

            isUsingFullBG = newUsingFullBg;
            if (isUsingFullBG)
            {
                fullbg.gameObject.SetActive(true);
                fullbg.color = setting.full_bg_color;
            }
        }
    }
}
