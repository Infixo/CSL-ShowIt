using System;
using System.Reflection;
using UnityEngine;
using ColossalFramework.UI;
using ICities;
using CitiesHarmony.API;

namespace ShowIt2
{
    public class ShowIt2Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => "Show It 2! (wip)";
        public string Description => "Shows service coverage, land value and details about leveling for zoned buildings.";

        private static GameObject m_goPanel;
        //private LoadMode _loadMode;
        public static ShowIt2Panel Panel { get { return m_goPanel?.GetComponent<ShowIt2Panel>(); } }

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => ShowIt2Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) ShowIt2Patcher.UnpatchAll();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame || mode == LoadMode.NewGameFromScenario)
            {
                m_goPanel = new GameObject("ShowIt2GameObject");
                m_goPanel.AddComponent<ShowIt2Panel>();
            }
        }

        public override void OnLevelUnloading()
        {
            if (m_goPanel != null)
            {
                UnityEngine.Object.Destroy(m_goPanel);
                m_goPanel = null;
            }
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

        private UITextField m_uiText;

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group;

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            group = helper.AddGroup(Name + " - " + assemblyName.Version.Major + "." + assemblyName.Version.Minor);

            int selectedIndex;
            float selectedValue;

            selectedIndex = GetSelectedOptionIndex(PanelAlignmentValues, ShowIt2Config.Instance.Alignment);
            group.AddDropdown("Alignment", PanelAlignmentLabels, selectedIndex, sel =>
            {
                ShowIt2Config.Instance.Alignment = PanelAlignmentValues[sel];
                ShowIt2Config.Instance.Save();
                Panel?.UpdateUI();
            });

            m_uiText = (UITextField)group.AddTextfield("Panel", "", callback => { return; });
            m_uiText.isEnabled = false; // removes an outline and makes it read-only

            selectedValue = ShowIt2Config.Instance.Scaling;
            group.AddSlider("Scaling", 0.5f, 1.5f, 0.1f, selectedValue, sel =>
            {
                ShowIt2Config.Instance.Scaling = sel;
                m_uiText.text = $"scaling={ShowIt2Config.Instance.Scaling} spacing={ShowIt2Config.Instance.Spacing}";
                ShowIt2Config.Instance.Save();
                Panel?.UpdateUI();
            });

            selectedValue = ShowIt2Config.Instance.Spacing;
            group.AddSlider("Chart Vertical Spacing", 0f, 5f, 1f, selectedValue, sel =>
            {
                ShowIt2Config.Instance.Spacing = sel;
                m_uiText.text = $"scaling={ShowIt2Config.Instance.Scaling} spacing={ShowIt2Config.Instance.Spacing}";
                ShowIt2Config.Instance.Save();
                Panel?.UpdateUI();
            });

            m_uiText.text = $"scaling={ShowIt2Config.Instance.Scaling} spacing={ShowIt2Config.Instance.Spacing}";
        }

        private int GetSelectedOptionIndex(string[] option, string value)
        {
            int index = Array.IndexOf(option, value);
            if (index < 0) index = 0;

            return index;
        }
    }
	
    [ConfigurationPath("ShowIt2Config.xml")]
    public class ShowIt2Config
    {
        public bool ShowPanel { get; set; } = true;
        public string Alignment { get; set; } = "Right";
        public float Scaling { get; set; } = 0.8f;
        public float Spacing { get; set; } = 2f;

        private static ShowIt2Config instance;

        public static ShowIt2Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Configuration<ShowIt2Config>.Load();
                }

                return instance;
            }
        }

        public void Save()
        {
            Configuration<ShowIt2Config>.Save();
        }
    }
	
}
