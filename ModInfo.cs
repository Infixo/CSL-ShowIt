using ICities;
using System;
using System.Reflection;
using ColossalFramework.UI;
using CitiesHarmony.API;

namespace ShowIt2
{
    public class ModInfo : IUserMod
    {
        public string Name => "Show It! 2";
        public string Description => "Shows service coverage, land value and details about leveling for zoned buildings.";
		
        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => ShowIt2Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) ShowIt2Patcher.UnpatchAll();
        }

        private static readonly string[] PanelAlignmentLabels =
        {
            "Right",
            "Bottom"
        };

        private static readonly string[] PanelAlignmentValues =
        {
            "Right",
            "Bottom"
        };

        /*private static readonly string[] IndicatorsPanelLegendLabels =
         {
            "Icons",
            "Labels",
            "Both"
        };

        private static readonly string[] IndicatorsPanelLegendValues =
        {
            "Icons",
            "Labels",
            "Both"
        };*/

        private UITextField m_uiText;

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group;

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            group = helper.AddGroup(Name + " - " + assemblyName.Version.Major + "." + assemblyName.Version.Minor);

            int selectedIndex;
            float selectedValue;

            selectedIndex = GetSelectedOptionIndex(PanelAlignmentValues, ModConfig.Instance.Alignment);
            group.AddDropdown("Alignment", PanelAlignmentLabels, selectedIndex, sel =>
            {
                ModConfig.Instance.Alignment = PanelAlignmentValues[sel];
                ModConfig.Instance.Save();
            });

            m_uiText = (UITextField)group.AddTextfield("Panel", "", callback => { return; });
            m_uiText.isEnabled = false; // removes an outline and makes it read-only

            selectedValue = ModConfig.Instance.Scaling;
            group.AddSlider("Scaling", 0.5f, 1.5f, 0.1f, selectedValue, sel =>
            {
                ModConfig.Instance.Scaling = sel;
                m_uiText.text = $"scaling={ModConfig.Instance.Scaling} spacing={ModConfig.Instance.Spacing}";
                ModConfig.Instance.Save();
            });

            /*selectedValue = ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing;
            group.AddSlider("Chart Horizontal Spacing", 5f, 25f, 1f, selectedValue, sel =>
            {
                ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing = sel;
                ModConfig.Instance.Save();
            });*/

            selectedValue = ModConfig.Instance.Spacing;
            group.AddSlider("Chart Vertical Spacing", 0f, 5f, 1f, selectedValue, sel =>
            {
                ModConfig.Instance.Spacing = sel;
                m_uiText.text = $"scaling={ModConfig.Instance.Scaling} spacing={ModConfig.Instance.Spacing}";
                ModConfig.Instance.Save();
            });

            m_uiText.text = $"scaling={ModConfig.Instance.Scaling} spacing={ModConfig.Instance.Spacing}";

            /*selectedValue = ModConfig.Instance.IndicatorsPanelNumberTextScale;
            group.AddSlider("Number Text Scale", 0.5f, 0.9f, 0.05f, selectedValue, sel =>
            {
                ModConfig.Instance.IndicatorsPanelNumberTextScale = sel;
                ModConfig.Instance.Save();
            });

            selectedIndex = GetSelectedOptionIndex(IndicatorsPanelLegendValues, ModConfig.Instance.IndicatorsPanelLegend);
            group.AddDropdown("Legend", IndicatorsPanelLegendLabels, selectedIndex, sel =>
            {
                ModConfig.Instance.IndicatorsPanelLegend = IndicatorsPanelLegendValues[sel];
                ModConfig.Instance.Save();
            });

            selectedValue = ModConfig.Instance.IndicatorsPanelIconSize;
            group.AddSlider("Icon Size", 15f, 25f, 0.5f, selectedValue, sel =>
            {
                ModConfig.Instance.IndicatorsPanelIconSize = sel;
                ModConfig.Instance.Save();
            });

            selectedValue = ModConfig.Instance.IndicatorsPanelLabelTextScale;
            group.AddSlider("Label Text Scale", 0.3f, 0.9f, 0.05f, selectedValue, sel =>
            {
                ModConfig.Instance.IndicatorsPanelLabelTextScale = sel;
                ModConfig.Instance.Save();
            });*/
        }

        private int GetSelectedOptionIndex(string[] option, string value)
        {
            int index = Array.IndexOf(option, value);
            if (index < 0) index = 0;

            return index;
        }
    }
}
