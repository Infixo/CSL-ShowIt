using ColossalFramework.UI;
using System.ComponentModel;
using System.Reflection.Emit;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace ShowIt
{
    public class UIUtils
        // Infixo todo: create slider for showing progress with level tresholds
        // Infixo todo: create slider for showing value of the immaterial resource effect
    {
        public static UIFont GetUIFont(string name)
        {
            UIFont[] fonts = Resources.FindObjectsOfTypeAll<UIFont>();

            foreach (UIFont font in fonts)
            {
                if (font.name.CompareTo(name) == 0)
                {
                    return font;
                }
            }

            return null;
        }

        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            UIPanel panel = parent.AddUIComponent<UIPanel>();
            panel.name = name;

            return panel;
        }

        public static UISprite CreateSprite(UIComponent parent, string name, string spriteName)
        {
            UISprite sprite = parent.AddUIComponent<UISprite>();
            sprite.name = name;
            sprite.spriteName = spriteName;

            return sprite;
        }

        public static UILabel CreateLabel(UIComponent parent, string name, string text)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.textAlignment = UIHorizontalAlignment.Center; // Infixo: looks like a default for all labels
            label.name = name;
            label.text = text;

            return label;
        }

        public static UICheckBox CreateCheckBox(UIComponent parent, string name, string text, bool state)
        {
            UICheckBox checkBox = parent.AddUIComponent<UICheckBox>();
            checkBox.name = name;

            checkBox.height = 16f;
            checkBox.width = parent.width - 10f;

            UISprite uncheckedSprite = checkBox.AddUIComponent<UISprite>();
            uncheckedSprite.spriteName = "check-unchecked";
            uncheckedSprite.size = new Vector2(16f, 16f);
            uncheckedSprite.relativePosition = Vector3.zero;

            UISprite checkedSprite = checkBox.AddUIComponent<UISprite>();
            checkedSprite.spriteName = "check-checked";
            checkedSprite.size = new Vector2(16f, 16f);
            checkedSprite.relativePosition = Vector3.zero;
            checkBox.checkedBoxObject = checkedSprite;

            checkBox.label = checkBox.AddUIComponent<UILabel>();
            checkBox.label.text = text;
            checkBox.label.font = GetUIFont("OpenSans-Regular");
            checkBox.label.autoSize = false;
            checkBox.label.height = 20f;
            checkBox.label.verticalAlignment = UIVerticalAlignment.Middle;
            checkBox.label.relativePosition = new Vector3(20f, 0f);

            checkBox.isChecked = state;

            return checkBox;
        }

        public static UIRadialChart CreateTwoSlicedRadialChart(UIComponent parent, string name)
        {
            UIRadialChart radialChart = parent.AddUIComponent<UIRadialChart>();
            radialChart.name = name;

            radialChart.size = new Vector3(50f, 50f);
            radialChart.spriteName = "PieChartBg";

            radialChart.AddSlice();
            UIRadialChart.SliceSettings slice = radialChart.GetSlice(0);
            Color32 color = new Color32(229, 229, 229, 128);
            slice.outterColor = color;
            slice.innerColor = color;

            radialChart.AddSlice();
            UIRadialChart.SliceSettings slice1 = radialChart.GetSlice(1);
            Color32 color1 = new Color32(178, 178, 178, 128);
            slice1.outterColor = color1;
            slice1.innerColor = color1;

            return radialChart;
        }
    }

    // UI control to show the service coverage using a progress bar
    // The value is shown propotionally to the max-value
    public class UIServiceBar
    {
        private const float DEFAULT_SCALE = 0.8f;
        private const float TEXT_WIDTH = 45f + 120f * DEFAULT_SCALE;
        private const float VALUE_WIDTH = 7f + 16f * DEFAULT_SCALE;
        public const float DEFAULT_HEIGHT = 1f + 17f * DEFAULT_SCALE;
        // internals
        private Color m_negativeColor = Color.red;
        private Color m_positiveColor = Color.green;
        private Color m_belowMidColor = Color.yellow;
        private Color m_progressColor = Color.blue;
        private Color m_completeColor = Color.green;
        private Color m_disabledColor = Color.gray;
        //private bool m_disabled = false;
        private bool m_negative = false;
        //private string m_text = "Text";
        private float m_value = 0f;
        private float m_maxValue = 0.5f; // progress bar filled 100%
        private float m_limit = 1f; // possible maxValue that will fill 100% entire control
        private bool m_belowMid = false;
        // ui components
        private UIPanel m_uiPanel;
        private UILabel m_uiTextLabel;
        private UILabel m_uiValueLabel;
        private UIProgressBar m_uiValueBar;
        private UILabel m_uiMaxValueLabel;

        // properties
        public UIPanel Panel { get { return m_uiPanel; } }

        public string Text
        {
            set { m_uiTextLabel.text = value; }
        }

        public float Value
        {
            get { return m_value; }
            set
            {
                m_value = value; UpdateControl();
                //m_uiValueLabel.text = m_value.ToString();
                //m_uiValueBar.value = Mathf.Max(0f, Mathf.Min(1f, m_value / m_maxValue)); // value sets the filled part, as 0.0f to 1.0f
                //m_uiValueBar.progressColor = ( m_value < m_maxValue ? m_progressColor : m_completeColor );
            }
        }

        public float MaxValue
        {
            get { return m_maxValue; }
            set
            {
                if (value <= 0f)
                {
                    Debug.Log("ShowIt.UIServiceBar.MaxValue: warning, trying to set MaxValue to <= 0!");
                    return;
                }
                m_maxValue = value;
                UpdateControl();
            }
        }

        public bool BelowMid
        {
            set { m_belowMid = value; UpdateControl(); }
        }

        public float Limit
        {
            get { return m_limit; }
            set
            {
                if (value <= 0f)
                {
                    Debug.Log("ShowIt.UIServiceBar.Limit: warning, trying to set Limit to <= 0!");
                    return;
                }
                m_limit = value;
                UpdateControl();
            }
        }
        
        public float Width
        {          
            set 
            { 
                m_uiPanel.width = value;
                UpdateControl();
            }
        }

        public bool Negative 
        { 
            set { m_negative = value; UpdateControl(); } 
        }

        // methods

        public UIServiceBar(UIComponent parent, string name)
        {
            // panel
            m_uiPanel = parent.AddUIComponent<UIPanel>();
            m_uiPanel.name = name;
            m_uiPanel.height = 20f;

            // text
            m_uiTextLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiTextLabel.name = name + "Text";
            m_uiTextLabel.textAlignment = UIHorizontalAlignment.Left;
            m_uiTextLabel.relativePosition = new Vector3(0, 0);
            m_uiTextLabel.textScale = DEFAULT_SCALE; // Infixo todo: connect with options
            m_uiTextLabel.disabledTextColor = m_disabledColor;

            // value
            m_uiValueLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiValueLabel.name = name + "Value";
            m_uiValueLabel.width = VALUE_WIDTH;
            m_uiValueLabel.textAlignment = UIHorizontalAlignment.Left;
            m_uiValueLabel.relativePosition = new Vector3(TEXT_WIDTH, 0);
            m_uiValueLabel.textScale = DEFAULT_SCALE; // Infixo todo: connect with options
            m_uiValueLabel.disabledTextColor = m_disabledColor;

            // bar
            m_uiValueBar = m_uiPanel.AddUIComponent<UIProgressBar>();
            m_uiValueBar.name = name + "ValueBar";
            m_uiValueBar.relativePosition = new Vector3(TEXT_WIDTH + VALUE_WIDTH, 0);
            m_uiValueBar.width = 200;
            m_uiValueBar.height = DEFAULT_HEIGHT - 2f; // Infixo todo: options
            m_uiValueBar.progressColor = m_progressColor;
            m_uiValueBar.progressSprite = "LevelBarForeground"; // economy panel uses PlainWhite
            m_uiValueBar.backgroundSprite = "LevelBarBackground";
            m_uiValueBar.value = 0.5f;
            m_uiValueBar.disabledColor = m_disabledColor;

            // maxValue
            m_uiMaxValueLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiMaxValueLabel.name = name + "MaxValue";
            m_uiMaxValueLabel.width = VALUE_WIDTH;
            m_uiMaxValueLabel.textAlignment = UIHorizontalAlignment.Center;
            m_uiMaxValueLabel.relativePosition = new Vector3(370f, 0);
            m_uiMaxValueLabel.textScale = DEFAULT_SCALE; // Infixo todo: connect with options
            m_uiMaxValueLabel.disabledTextColor = m_disabledColor;
        }

        // Will resize the value bar and set colors according to current settings
        private void UpdateControl()
        {
            // texts
            m_uiValueLabel.text = m_value.ToString();
            m_uiMaxValueLabel.text = m_maxValue.ToString();
            // value bar
            float barMaxWidth = m_uiPanel.width - TEXT_WIDTH - VALUE_WIDTH - VALUE_WIDTH;
            m_uiValueBar.width = Mathf.Min(barMaxWidth * m_maxValue / m_limit, barMaxWidth); // width sets the size of the entire progress bar
            m_uiValueBar.value = Mathf.Max(0f, Mathf.Min(1f, m_value / m_maxValue)); // value sets the filled part, as 0.0f to 1.0f
            m_uiMaxValueLabel.relativePosition = new Vector3(TEXT_WIDTH + VALUE_WIDTH + m_uiValueBar.width + 2f, 0);
            // colors
            m_uiTextLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiValueLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiMaxValueLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiValueBar.progressColor = (m_negative ? m_negativeColor : (m_belowMid ? m_belowMidColor : m_progressColor));
            if (m_value >= m_maxValue && !m_negative) m_uiValueBar.progressColor = m_completeColor;
            // disabled
            //if (m_disabled)
            //{
                //m_uiTextLabel.textColor = m_disabledColor;
                //m_uiValueLabel.textColor = m_disabledColor;
                //m_uiMaxValueLabel.textColor = m_disabledColor;
            //}
        }
    }
}
