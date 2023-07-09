using System;
using System.Linq;
using UnityEngine;
using ColossalFramework.UI;

namespace ShowIt2
{
    public static class UIFonts // this gist is from from AlgernonCommons
    {
        private static UIFont m_regular;
        private static UIFont m_semiBold;

        public static UIFont Regular
        {
            get
            {
                if (m_regular == null)
                {
                    m_regular = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");
                }
                return m_regular;
            }
        }

        public static UIFont SemiBold
        {
            get
            {
                if (m_semiBold == null)
                {
                    m_semiBold = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Semibold");
                }
                return m_semiBold;
            }
        }
    }

    /*
    public static UISprite CreateSprite(UIComponent parent, string name, string spriteName)
    {
        UISprite sprite = parent.AddUIComponent<UISprite>();
        sprite.name = name;
        sprite.spriteName = spriteName;

        return sprite;
    }
    */

    // UI control to show the service coverage using a progress bar
    // The value is shown propotionally to the max-value
    public class UIServiceBar
    {
        //private const int SCALER = 13; // actual scale is * 0.0625 which is 1/16
        //private const float DEFAULT_SCALE = SCALER / 16f;
        //private const float TEXT_WIDTH = 28f + 133f * DEFAULT_SCALE; // longest is Firewatch Coverage :)
        //private const float VALUE_WIDTH = 6f + 18f * DEFAULT_SCALE; // calculated to accomodate numbers like 18.8
        public const float DEFAULT_HEIGHT = 18f; // * DEFAULT_SCALE;

        private Color m_negativeColor = Color.red;
        private Color m_positiveColor = Color.green;
        private Color m_belowMidColor = Color.yellow;
        private Color m_progressColor = new Color32(0, 231, 241, byte.MaxValue);
        private Color m_completeColor = Color.green;
        private Color m_disabledColor = Color.gray;

        // data
        private bool m_negative = false;
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
        private float m_uiScale = 13f / 16f; // default scale is 0.8125
        private bool m_uiFloats = true;
        private float m_uiValueWidth = 26f;
        private float m_uiTextWidth = 135f;

        // properties

        public UIPanel Panel { get { return m_uiPanel; } }

        public string Text { set { m_uiTextLabel.text = value; } }

        public float Value { set { m_value = value; UpdateControl(); } }

        public float MaxValue
        {
            set
            {
                if (value <= 0f)
                {
                    Debug.Log("ShowIt2.UIServiceBar.MaxValue: WARNING trying to set MaxValue to <= 0!");
                    return;
                }
                m_maxValue = value;
                UpdateControl();
            }
        }

        public bool BelowMid { set { m_belowMid = value; UpdateControl(); } }

        public float Limit
        {
            set
            {
                if (value <= 0f)
                {
                    Debug.Log("ShowIt2.UIServiceBar.Limit: WARNING trying to set Limit to <= 0!");
                    return;
                }
                m_limit = value;
                UpdateControl();
            }
        }
        
        public float Width { set { m_uiPanel.width = value; UpdateControl(); } }

        public bool Negative { set { m_negative = value; UpdateControl(); } }

        public void ShowInts()
        {
            m_uiFloats = false;
            m_uiValueWidth = 16f * m_uiScale + 4f; // scaled to fit "88"
        }

        public void ShowFloats()
        {
            m_uiFloats = true;
            m_uiValueWidth = 25f * m_uiScale + 6f; // scaled to fit "18.8"
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
            m_uiTextLabel.font = UIFonts.Regular;
            m_uiTextLabel.textAlignment = UIHorizontalAlignment.Left;
            m_uiTextLabel.relativePosition = new Vector3(0f, 0f);
            m_uiTextLabel.textScale = m_uiScale;
            m_uiTextLabel.disabledTextColor = m_disabledColor;

            // value
            m_uiValueLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiValueLabel.name = name + "Value";
            m_uiValueLabel.font = UIFonts.Regular;
            m_uiValueLabel.relativePosition = new Vector3(m_uiTextWidth, 0f);
            m_uiValueLabel.textScale = m_uiScale;
            m_uiValueLabel.disabledTextColor = m_disabledColor;

            // bar
            m_uiValueBar = m_uiPanel.AddUIComponent<UIProgressBar>();
            m_uiValueBar.name = name + "ValueBar";
            m_uiValueBar.relativePosition = new Vector3(m_uiTextWidth + m_uiValueWidth, 0);
            m_uiValueBar.width = 200f;
            m_uiValueBar.height = DEFAULT_HEIGHT * m_uiScale - 1f;
            m_uiValueBar.progressColor = m_progressColor;
            m_uiValueBar.progressSprite = "LevelBarForeground"; // same as LevelBar in the ZonedInfoView
            m_uiValueBar.backgroundSprite = "LevelBarBackground";
            m_uiValueBar.fillMode = UIFillMode.Fill; // this is IMPORTANT! default is Stretch which causes 0 to be shown as a thin bar!
            m_uiValueBar.value = 0.5f;
            m_uiValueBar.disabledColor = m_disabledColor;

            // maxValue
            m_uiMaxValueLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiMaxValueLabel.name = name + "MaxValue";
            m_uiMaxValueLabel.font = UIFonts.Regular;
            m_uiMaxValueLabel.width = m_uiValueWidth;
            m_uiMaxValueLabel.textAlignment = UIHorizontalAlignment.Center;
            m_uiMaxValueLabel.relativePosition = new Vector3(370f, 0);
            m_uiMaxValueLabel.textScale = m_uiScale;
            m_uiMaxValueLabel.disabledTextColor = m_disabledColor;
        }

        public void SetScale(float scale)
        {
            m_uiScale = scale;
            m_uiTextLabel.textScale = scale;
            m_uiValueLabel.textScale = scale;
            m_uiMaxValueLabel.textScale = scale;
            m_uiValueBar.height = DEFAULT_HEIGHT * scale - 1f;
            m_uiTextWidth = 28f + 133f * scale;
            if (m_uiFloats) ShowFloats(); else ShowInts();
            UpdateControl();
        }

        // Will resize the value bar and set colors according to current settings
        private void UpdateControl()
        {
            // texts
            m_uiValueLabel.text = m_value.ToString();
            m_uiMaxValueLabel.text = m_maxValue.ToString();
            // value bar
            float barMaxWidth = m_uiPanel.width - m_uiTextWidth - m_uiValueWidth - m_uiValueWidth - 2f;
            m_uiValueBar.width = Mathf.Min(barMaxWidth * m_maxValue / m_limit, barMaxWidth); // width sets the size of the entire progress bar
            m_uiValueBar.value = Mathf.Max(0f, Mathf.Min(1f, m_value / m_maxValue)); // value sets the filled part, as 0.0f to 1.0f
            // scaling
            m_uiValueLabel.relativePosition = new Vector3(m_uiTextWidth, 0f);
            m_uiValueBar.relativePosition = new Vector3(m_uiTextWidth + m_uiValueWidth, 0f);
            m_uiMaxValueLabel.relativePosition = new Vector3(m_uiTextWidth + m_uiValueWidth + m_uiValueBar.width + 2f, 0);
            // colors
            m_uiTextLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiValueLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiMaxValueLabel.textColor = (m_negative ? m_negativeColor : m_positiveColor);
            m_uiValueBar.progressColor = (m_negative ? m_negativeColor : (m_belowMid ? m_belowMidColor : m_progressColor));
            if (m_value >= m_maxValue && !m_negative) m_uiValueBar.progressColor = m_completeColor;
        }

    } // UIServiceBar

    // UI control to show the level progress using multiple (max. 5) progress bars of various size
    // The value is shown propotionally to the max value
    // It will be placed on the ZonedBuilding info panel, so its size will be constant
    public class UILevelProgress
    {
        private const int MAX_PARTS = 5;
        private const float DEFAULT_SCALE = 0.8125f; // 0.8125f is 13/16 used as normal text
        private const float SMALL_SCALE = 0.625f; // 0.625f is 10/16 used in small text on the ZonedInfo panel
        private const float TEXT_WIDTH = 80f; // 45f + 120f * DEFAULT_SCALE;
        private const float VALUE_WIDTH = 6f + 18f * DEFAULT_SCALE;
        public const float DEFAULT_HEIGHT = 13f; // 1f + 17f * DEFAULT_SCALE;

        private Color m_progressColor = new Color32(0, 231, 241, byte.MaxValue);
        private Color m_completeColor = Color.green;

        // data
        private float m_value = 0f;
        private readonly float[] m_maxValues = new float[MAX_PARTS]; // progress bar filled 100%
        private int m_parts = MAX_PARTS;

        // ui components
        private UIPanel m_uiPanel;
        private UILabel m_uiTextLabel;
        private UILabel m_uiValueLabel;
        private UIProgressBar[] m_uiValueBars = new UIProgressBar[MAX_PARTS];
        private UILabel[] m_uiMidLabels = new UILabel[MAX_PARTS]; // last one will not be shown, but it is easier to iterate like this
        private UISprite m_icon;

        // properties

        public UIPanel Panel { get { return m_uiPanel; } } // this could be component in UICustomControl

        public string Text { set { m_uiTextLabel.text = value; } }

        public float Value { set { m_value = value; m_uiValueLabel.text = m_value.ToString();  UpdateControl(); } }

        public float[] MaxValues
        {
            set
            {
                m_parts = Math.Max(1, Math.Min(value.Length, MAX_PARTS));
                for (int i = 0; i < m_parts; i++)
                {
                    if (value[i] <= 0f)
                    {
                        Debug.Log("ShowIt2.UILevelProgress.MaxValues: WARNING trying to set MidValue to <= 0!");
                        return;
                    }
                    if (i > 0 && value[i] <= value[i - 1])
                    {
                        Debug.Log("ShowIt2.UILevelProgress.MaxValues: WARNING trying to set MidValue lower that pevious one");
                        return;
                    }
                }
                for (int i = 0; i < m_parts; i++)
                    m_maxValues[i] = value[i];
                UpdateControl();
            }
        }
        public float Width { set { m_uiPanel.width = value; UpdateControl(); } }

        public bool Happy { set { m_icon.spriteName = "NotificationIcon" + (value ? "Happy" : "Unhappy"); } }

        public UILevelProgress(UIComponent parent, string name)
        {
            // panel
            m_uiPanel = parent.AddUIComponent<UIPanel>();
            m_uiPanel.name = name;
            m_uiPanel.height = 40f; // 14f + 10f + 11f

            // text
            m_uiTextLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiTextLabel.name = "Text";
            m_uiTextLabel.font = UIFonts.Regular;
            m_uiTextLabel.relativePosition = new Vector3(0, 0);
            m_uiTextLabel.textScale = 0.75f; // 12/16

            // value
            m_uiValueLabel = m_uiPanel.AddUIComponent<UILabel>();
            m_uiValueLabel.name = "Value";
            m_uiValueLabel.font = UIFonts.Regular;
            m_uiValueLabel.width = VALUE_WIDTH;
            m_uiValueLabel.relativePosition = new Vector3(TEXT_WIDTH, -1f);
            m_uiValueLabel.textScale = 0.8125f; // 13/16

            // bars
            for (int i = 0; i < MAX_PARTS; i++)
            {
                UIProgressBar bar = m_uiPanel.AddUIComponent<UIProgressBar>();
                bar.name = "Bar" + i;
                bar.relativePosition = new Vector3(50f * i, DEFAULT_HEIGHT);
                bar.width = 50f;
                bar.height = 12f; // ZonedInfo's Level bar is 14f
                bar.progressColor = m_progressColor;
                bar.progressSprite = "LevelBarForeground"; // economy panel uses PlainWhite
                bar.backgroundSprite = "LevelBarBackground";
                bar.fillMode = UIFillMode.Fill; // IMPORTANT! otherwise shows 0 as a thin bar!
                bar.value = 0.5f;
                m_uiValueBars[i] = bar;
            }

            // midValue labels
            for (int i = 0; i < MAX_PARTS; i++)
            {
                UILabel lbl = m_uiPanel.AddUIComponent<UILabel>();
                lbl.name = "MidLabel" + i;
                lbl.font = UIFonts.Regular;
                lbl.width = VALUE_WIDTH;
                lbl.relativePosition = new Vector3(50f * (i+1), DEFAULT_HEIGHT + 12f + 2f);
                lbl.textScale = SMALL_SCALE;
                m_uiMidLabels[i] = lbl;
            }

            // icon
            m_icon = m_uiPanel.AddUIComponent<UISprite>();
            m_icon.name = "Icon";
            m_icon.size = new Vector2(20f, 20f);
            m_icon.relativePosition = new Vector3(-23f, 3f);
            m_icon.spriteName = "NotificationIconHappy"; // "NotificationIcon" + { "VeryUnhappy", "Unhappy", "Happy", "VeryHappy", "ExtremelyHappy" };
        }

        private void UpdateControl()
        {
            float maxValue = m_maxValues[m_parts-1]; // actual max value that represents all bars 100% filled
            float scale = m_uiPanel.width / maxValue;

            // iterate through bars, resize accordingly, place labels
            float posx = 0f;
            for (int i = 0; i < m_parts; i++)
            {
                UIProgressBar bar = m_uiValueBars[i];
                bar.width = m_maxValues[i] * scale - posx;
                bar.relativePosition = new Vector3(posx, DEFAULT_HEIGHT);
                posx += bar.width; // next bar, also the label
                UILabel lbl = m_uiMidLabels[i];
                lbl.relativePosition = new Vector3(posx, DEFAULT_HEIGHT + DEFAULT_HEIGHT + 2f);
                lbl.text = m_maxValues[i].ToString();
            }

            // iterate through bars, show progress accordingly
            float shown = 0f; // how much already shown
            for (int i = 0; i < MAX_PARTS; i++)
            {
                UIProgressBar bar = m_uiValueBars[i];
                if (i < m_parts)
                {
                    bar.Show();
                    m_uiMidLabels[i].Show();
                    float curMax = m_maxValues[i];
                    if (m_value < shown) // not filled bars
                        bar.value = 0f;
                    else if (m_value < curMax) //current partially filled bar
                    {
                        bar.value = (m_value - shown) / (curMax - shown);
                        bar.progressColor = m_progressColor;
                    }
                    else // completed bar
                    {
                        bar.value = 1f;
                        bar.progressColor = m_completeColor;
                    }
                    shown = curMax;
                }
                else
                {
                    bar.Hide();
                    m_uiMidLabels[i].Hide();
                }
            }

            m_uiMidLabels[m_parts-1].Hide(); // last one is always hidden
        }

    } // UILevelProgress

}