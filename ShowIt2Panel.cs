using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Globalization;
using Epic.OnlineServices.Connect;
using ColossalFramework.Plugins;
using System.Reflection;
using ICities;
using static NetInfo;
using static Citizen;
//using static System.Net.Mime.MediaTypeNames;
//using UnityEngine.SocialPlatforms;
//using static Citizen;
//using static PathUnit;

namespace ShowIt2
{

    public class ShowIt2Panel : MonoBehaviour
    {
        public const ImmaterialResourceManager.Resource WaterProximity = (ImmaterialResourceManager.Resource)253; // Water proximity does not exists in IRM but it is used to calculate land value
        public const ImmaterialResourceManager.Resource GroundPollution = (ImmaterialResourceManager.Resource)254; // Ground Pollution does not exists in IRM but it is used to calculate service coverage value

        //private bool m_hardMode = false;

        public enum Zone { R = 0, I, C, O };

        private readonly float[][] MAX_VALUES_TOPBAR = new float[][] {
            // education and wealth are not changed by HardMode
            new float[] { 15f, 30f, 45f, 60f, 75f }, // R
            new float[] { 15f, 30f, 45f }, // I
            new float[] { 30f, 45f, 60f }, // C
            new float[] { 15f, 30f, 45f }, // O
        };

        private readonly float[][] MAX_VALUES_NORMAL = new float[][] {
            new float[] { 6f, 21f, 41f, 61f, 75f }, // Residential, land value
            new float[] { 30f, 60f, 90f }, // Industrial, services
            new float[] { 21f, 41f, 61f }, // Commercial, land value
            new float[] { 45f, 90f, 135f }, // Office, services
        };

        private readonly float[][] MAX_VALUES_HARDMODE = new float[][] {
            // service coverage and land values are higher in hard mode
            new float[] { 15f, 35f, 60f, 85f, 105f }, // Residential, land value
            new float[] { 40f, 80f, 120f }, // Industrial, services
            new float[] { 30f, 60f, 90f }, // Commercial, land value
            new float[] { 55f, 110f, 165f }, // Office, services
        };

        private float[][] MAX_VALUES_BOTBAR;

        private ZonedBuildingWorldInfoPanel m_uiZonedBuildingWorldInfoPanel;
        private UICheckBox m_showPanelCheckBox;
        private UIPanel m_uiMainPanel;
        // leveling progress - wealth, education section
        private UILevelProgress m_topBar;
        // leveling progress - land value, service coverage
        private UILevelProgress m_botBar;
        // resource value bars
        private Dictionary<ImmaterialResourceManager.Resource, UIServiceBar> m_uiServices = new Dictionary<ImmaterialResourceManager.Resource, UIServiceBar>();
        private UIServiceBar[] m_uiBarPosition = new UIServiceBar[25];

        public void Start() => CreateUI();

        private UICheckBox CreateCheckBox(UIComponent parent, string name, string text, bool state)
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
            //checkBox.label.font = GetUIFont("OpenSans-Regular");
            checkBox.label.autoSize = false;
            checkBox.label.height = 20f;
            checkBox.label.verticalAlignment = UIVerticalAlignment.Middle;
            checkBox.label.relativePosition = new Vector3(20f, 0f);

            checkBox.isChecked = state;

            return checkBox;
        }

        private void CreateUIServiceBar(UIComponent parent, int position, ImmaterialResourceManager.Resource resource)
        {
            UIServiceBar uiServiceBar = new UIServiceBar(parent, "ShowIt2" + resource.ToString() + "ServiceBar");
            uiServiceBar.Width = 440f; // width of the parent is 0 atm
            //uiServiceBar.Panel.relativePosition = new Vector3(10f, 60f + position * (UIServiceBar.DEFAULT_HEIGHT + 2f)); // Infixo todo: scaling
            uiServiceBar.Panel.relativePosition = new Vector3(10f, 60f + position * (UIServiceBar.DEFAULT_HEIGHT * ShowIt2Config.Instance.Scaling + ShowIt2Config.Instance.Spacing)); // Infixo todo: scaling
            uiServiceBar.Text = GetResourceName(resource);
            uiServiceBar.Limit = 60f; // Biggest is Cargo 100, then Fire 50, others are <= 33
            uiServiceBar.MaxValue = 20f;
            m_uiServices.Add(resource, uiServiceBar);
            // disable the ones not available
            GetIndicatorInfoModes(resource, out InfoManager.InfoMode infoMode, out InfoManager.SubInfoMode subInfoMode);
            if (resource != GroundPollution && resource != ImmaterialResourceManager.Resource.Abandonment) // there is no UI overlay for Abandonment
                uiServiceBar.Panel.isEnabled = Singleton<InfoManager>.instance.IsInfoModeAvailable(infoMode);
            // store in a orderly fashion to allow for rescaling later
            m_uiBarPosition[position] = uiServiceBar;
        }

        private void RescaleServiceBars(float limit)
        {
            foreach (UIServiceBar uiBar in m_uiServices.Values)
                uiBar.Limit = limit;
        }

        public void CreateUI()
        {
            MAX_VALUES_BOTBAR = MAX_VALUES_NORMAL;
            // Check if HardMode plugin is ON
            // item.name - it is folder name where the mod resides, e.g. SteamID for workshop mods
            // (item.userModInstance as IUserMod).Name - mod's native name
            foreach (PluginManager.PluginInfo item in Singleton<PluginManager>.instance.GetPluginsInfo())
                if (item.isBuiltin && item.isEnabled && item.name == "HardMode")
                {
                    Debug.Log($"ShowIt2: HardMode enabled");
                    MAX_VALUES_BOTBAR = MAX_VALUES_HARDMODE;
                }

            m_uiZonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();

            m_uiMainPanel = m_uiZonedBuildingWorldInfoPanel.component.AddUIComponent<UIPanel>();
            m_uiMainPanel.name = "ShowIt2Panel";
            m_uiMainPanel.backgroundSprite = "SubcategoriesPanel";
            m_uiMainPanel.opacity = 0.90f;
            //m_uiMainPanel.height = 10f + (UIServiceBar.DEFAULT_HEIGHT * ShowIt2Config.Instance.Scaling + ShowIt2Config.Instance.Spacing) * 25 + 10f;
            m_uiMainPanel.width = m_uiZonedBuildingWorldInfoPanel.component.width;
            m_uiMainPanel.isVisible = ShowIt2Config.Instance.ShowPanel;

            // adjust a little Zoned Building Info
            //OverWorkSituation // this label overlaps => move 228x5 => 230x20
            UILabel overWorkSituation = m_uiZonedBuildingWorldInfoPanel.Find<UILabel>("OverWorkSituation");
            overWorkSituation.relativePosition = new Vector3(230f, 20f); // shift down
            UISprite specPolicyIcon = m_uiZonedBuildingWorldInfoPanel.Find<UISprite>("SpecializationPolicyIcon");
            specPolicyIcon.relativePosition = new Vector3(specPolicyIcon.relativePosition.x, specPolicyIcon.relativePosition.y-4f); // shift up

            // checkbox to toggle on/off the extra panel
            UIPanel _makeHistoricalPanel = m_uiZonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
            m_showPanelCheckBox = CreateCheckBox(_makeHistoricalPanel, "ShowIt2ShowPanelCheckBox", "Indicators", ShowIt2Config.Instance.ShowPanel);
            m_showPanelCheckBox.width = 110f;
            m_showPanelCheckBox.label.textColor = new Color32(185, 221, 254, 255);
            m_showPanelCheckBox.label.textScale = 0.8125f;
            m_showPanelCheckBox.label.font = UIFonts.Regular;
            m_showPanelCheckBox.tooltip = "Indicators show how well serviced the building is (for Industrial and Office) or what contributes to the Land Value (for Residential and Commercial)";
            //m_showPanelCheckBox.AlignTo(_makeHistoricalPanel, UIAlignAnchor.TopLeft);
            m_showPanelCheckBox.relativePosition = new Vector3(_makeHistoricalPanel.width - m_showPanelCheckBox.width, 6f);
            m_showPanelCheckBox.eventCheckChanged += (component, value) =>
            {
                m_uiMainPanel.isVisible = value;
                ShowIt2Config.Instance.ShowPanel = value;
                ShowIt2Config.Instance.Save();
                UpdateUI();
            };

            // leveling progress - wealth, education section
            m_topBar = new UILevelProgress(m_uiZonedBuildingWorldInfoPanel.component, "ShowIt2TopLevelBar");
            m_topBar.Width = 185f;
            m_topBar.Panel.relativePosition = new Vector3(255f, 104f);

            // leveling progress - land value, service coverage
            m_botBar = new UILevelProgress(m_uiZonedBuildingWorldInfoPanel.component, "ShowIt2BotLevelBar");
            m_botBar.Width = 185f;
            m_botBar.Panel.relativePosition = new Vector3(255f, 142f);

            // service bars
            // UIComponent tree
            // main panel (UIPanel)-> attached via AddComponent<>
            // ...singular controls
            // ...service bars (UIPanel)
            // ......sub-controls
            CreateUIServiceBar(m_uiMainPanel, 0, ImmaterialResourceManager.Resource.CargoTransport);
            CreateUIServiceBar(m_uiMainPanel, 1, ImmaterialResourceManager.Resource.PublicTransport);
            CreateUIServiceBar(m_uiMainPanel, 2, ImmaterialResourceManager.Resource.FireDepartment);
            CreateUIServiceBar(m_uiMainPanel, 3, ImmaterialResourceManager.Resource.PoliceDepartment);
            CreateUIServiceBar(m_uiMainPanel, 4, ImmaterialResourceManager.Resource.EducationElementary);
            CreateUIServiceBar(m_uiMainPanel, 5, ImmaterialResourceManager.Resource.EducationHighSchool);
            CreateUIServiceBar(m_uiMainPanel, 6, ImmaterialResourceManager.Resource.EducationUniversity);
            CreateUIServiceBar(m_uiMainPanel, 7, ImmaterialResourceManager.Resource.EducationLibrary); // land value
            CreateUIServiceBar(m_uiMainPanel, 8, ImmaterialResourceManager.Resource.HealthCare);
            CreateUIServiceBar(m_uiMainPanel, 9, ImmaterialResourceManager.Resource.DeathCare);
            CreateUIServiceBar(m_uiMainPanel, 10, ImmaterialResourceManager.Resource.ChildCare); // land value
            CreateUIServiceBar(m_uiMainPanel, 11, ImmaterialResourceManager.Resource.ElderCare); // land value
            CreateUIServiceBar(m_uiMainPanel, 12, ImmaterialResourceManager.Resource.Entertainment);
            CreateUIServiceBar(m_uiMainPanel, 13, ImmaterialResourceManager.Resource.PostService);
            CreateUIServiceBar(m_uiMainPanel, 14, ImmaterialResourceManager.Resource.FirewatchCoverage);
            CreateUIServiceBar(m_uiMainPanel, 15, ImmaterialResourceManager.Resource.RadioCoverage);
            CreateUIServiceBar(m_uiMainPanel, 16, ImmaterialResourceManager.Resource.DisasterCoverage);
            // dynamic - health, wellbeing
            CreateUIServiceBar(m_uiMainPanel, 17, ImmaterialResourceManager.Resource.Health); // land value
            CreateUIServiceBar(m_uiMainPanel, 18, ImmaterialResourceManager.Resource.Wellbeing); // land value
            CreateUIServiceBar(m_uiMainPanel, 19, WaterProximity); // land value
            // negatives
            CreateUIServiceBar(m_uiMainPanel, 20, ImmaterialResourceManager.Resource.Abandonment);
            CreateUIServiceBar(m_uiMainPanel, 21, ImmaterialResourceManager.Resource.NoisePollution);
            CreateUIServiceBar(m_uiMainPanel, 22, GroundPollution);
            CreateUIServiceBar(m_uiMainPanel, 23, ImmaterialResourceManager.Resource.FireHazard); // land value
            CreateUIServiceBar(m_uiMainPanel, 24, ImmaterialResourceManager.Resource.CrimeRate); // land value

            UpdateUI(); // initial placement and sizing

            // Realistic Population button => 282x78
            UIButton rpButton = m_uiZonedBuildingWorldInfoPanel.component.Find<UIButton>("UIButton");
            if (rpButton != null)
                rpButton.relativePosition = new Vector3(282f, 78f);
            else
                Debug.Log("ShowIt2.CreateUI: cannot find RP button");
            //UIButton b2 = m_uiZonedBuildingWorldInfoPanel.component.GetComponent<UIButton>();
        }

        // Resizes the panel and places the controls according to current settings
        public void UpdateUI()
        {
            if (ShowIt2Config.Instance.Alignment is "Right")
            {
                m_uiMainPanel.relativePosition = new Vector3(m_uiMainPanel.parent.width + 1f, 0f);
            }
            else
            {
                m_uiMainPanel.relativePosition = new Vector3(0f, m_uiMainPanel.parent.height - 14f); // there some issue with ZonedInfoView, its height is bigger that actual window
            }
            m_uiMainPanel.height = 10f + (UIServiceBar.DEFAULT_HEIGHT * ShowIt2Config.Instance.Scaling + ShowIt2Config.Instance.Spacing) * 25 + 10f;
            for (int i = 0; i < 25; i++)
            {
                m_uiBarPosition[i].Panel.relativePosition = new Vector3(10f, 10f + i * (UIServiceBar.DEFAULT_HEIGHT * ShowIt2Config.Instance.Scaling + ShowIt2Config.Instance.Spacing));
                m_uiBarPosition[i].SetScale(ShowIt2Config.Instance.Scaling);
            }
        }

        public void RefreshData()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            switch (building.Info.m_class.GetZone())
            {
                case ItemClass.Zone.ResidentialHigh:
                case ItemClass.Zone.ResidentialLow:
                    ShowResidentialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.Industrial:
                    ShowIndustrialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.CommercialHigh:
                case ItemClass.Zone.CommercialLow:
                    ShowCommercialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.Office:
                    ShowOfficeProgress(buildingID, ref building);
                    break;
            }
        }


        /*private bool IsIndicatorAvailable(ImmaterialResourceManager.Resource resource)
        {
            InfoManager.InfoMode infoMode = InfoManager.InfoMode.LandValue;
            InfoManager.SubInfoMode subInfoMode = InfoManager.SubInfoMode.Default;

            GetIndicatorInfoModes(resource, out infoMode, out subInfoMode);

            return Singleton<InfoManager>.instance.IsInfoModeAvailable(infoMode);

        }*/

        private static readonly Dictionary<ImmaterialResourceManager.Resource, string> LABELS = new Dictionary<ImmaterialResourceManager.Resource, string>
		{
			{ ImmaterialResourceManager.Resource.HealthCare,            LocaleID.INFO_HEALTH_HEALTHCARE },
			{ ImmaterialResourceManager.Resource.FireDepartment,        LocaleID.INFO_FIRE_TITLE },
			{ ImmaterialResourceManager.Resource.PoliceDepartment,      LocaleID.INFO_CRIMERATE_COVERAGE },
			{ ImmaterialResourceManager.Resource.EducationElementary,   LocaleID.INFO_EDUCATION_ELEMENTARY },
			{ ImmaterialResourceManager.Resource.EducationHighSchool,   LocaleID.INFO_EDUCATION_HIGH },
			{ ImmaterialResourceManager.Resource.EducationUniversity,   LocaleID.INFO_EDUCATION_UNIVERSITY },
			{ ImmaterialResourceManager.Resource.DeathCare,             LocaleID.INFO_HEALTH_DEATHCARE },
			{ ImmaterialResourceManager.Resource.PublicTransport,       "Public Transport" }, // INFO_PUBLICTRANSPORT_TITLE
			{ ImmaterialResourceManager.Resource.NoisePollution,        LocaleID.INFO_NOISEPOLLUTION_TITLE },
			{ ImmaterialResourceManager.Resource.CrimeRate,             LocaleID.INFO_CRIMERATE_METER },
			{ ImmaterialResourceManager.Resource.Health,                LocaleID.INFO_HEALTH_AVERAGE }, // INFO_HEALTH_HEALTH
			{ ImmaterialResourceManager.Resource.Wellbeing,             LocaleID.INFO_HAPPINESS_TITLE },
			{ ImmaterialResourceManager.Resource.Density,               LocaleID.INFO_POPULATION_DENSITY },
			{ ImmaterialResourceManager.Resource.Entertainment,         LocaleID.INFO_ENTERTAINMENT_TITLE },
			{ ImmaterialResourceManager.Resource.LandValue,             LocaleID.INFO_LANDVALUE_TITLE },
			{ ImmaterialResourceManager.Resource.Attractiveness,        LocaleID.INFO_TOURISM_ATTRACTIVENESS },
			{ ImmaterialResourceManager.Resource.Coverage,              LocaleID.INFO_FIRE_COVERAGE }, // not sure what this is
			{ ImmaterialResourceManager.Resource.FireHazard,            LocaleID.INFO_FIRE_METER }, // fire meter says "fire hazard"
			{ ImmaterialResourceManager.Resource.Abandonment,           LocaleID.BUILDING_STATUS_ABANDONED }, // there is no ui for that CHIRP_ABANDONED_BUILDINGS
			{ ImmaterialResourceManager.Resource.CargoTransport,        "Cargo Transport" }, // INFO_PUBLICTRANSPORT_TITLE
			{ ImmaterialResourceManager.Resource.RadioCoverage,         LocaleID.INFO_RADIO_SIGNAL }, // INFO_RADIO_BUILDINGS radio masts, INFO_RADIO_SIGNAL signal strength
			{ ImmaterialResourceManager.Resource.FirewatchCoverage,     "Firewatch Coverage" }, // INFO_FIRE_COVERAGE firefighter efficiency
			{ ImmaterialResourceManager.Resource.EarthquakeCoverage,    LocaleID.INFO_DETECTION_COVERAGE }, // earthquake detection
			{ ImmaterialResourceManager.Resource.DisasterCoverage,      "Disaster Coverage" }, // INFO_DISASTERRISK_DISASTERRISK distaster risk
			{ ImmaterialResourceManager.Resource.TourCoverage,          LocaleID.INFO_TOURS_TITLE },
			{ ImmaterialResourceManager.Resource.PostService,           LocaleID.INFO_POST_TITLE },
			{ ImmaterialResourceManager.Resource.EducationLibrary,      LocaleID.INFO_EDUCATION_LIBRARY },
			{ ImmaterialResourceManager.Resource.ChildCare,             LocaleID.INFO_HEALTH_CHILDCARE },
			{ ImmaterialResourceManager.Resource.ElderCare,             LocaleID.INFO_HEALTH_ELDERCARE },
			{ ImmaterialResourceManager.Resource.CashCollecting,        LocaleID.INFO_FINANCIAL_CASHCOLLECTINGCOVERAGE },
			{ ImmaterialResourceManager.Resource.TaxBonus,              LocaleID.INFO_FINANCIAL_TAXBONUS },
			{ ImmaterialResourceManager.Resource.Sightseeing,           LocaleID.INFO_HOTEL_SIGHTSEEING },
			{ ImmaterialResourceManager.Resource.Shopping,              LocaleID.INFO_HOTEL_SHOPPING },
			{ ImmaterialResourceManager.Resource.Business,              LocaleID.INFO_HOTEL_BUSINESS },
			{ ImmaterialResourceManager.Resource.Nature,                LocaleID.INFO_HOTEL_NATURE },
			{ ImmaterialResourceManager.Resource.None,                  LocaleID.INFO_NONE },
            {                                    WaterProximity,        LocaleID.INFO_WATER_TITLE },
			{                                    GroundPollution,       LocaleID.INFO_POLLUTION_TITLE },
		};

        private string GetResourceName(ImmaterialResourceManager.Resource resource)
        {
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.PublicTransport:
                case ImmaterialResourceManager.Resource.CargoTransport:
                case ImmaterialResourceManager.Resource.FirewatchCoverage:
                case ImmaterialResourceManager.Resource.DisasterCoverage:
                    return LABELS[resource];
                default:
                    return Locale.Get(LABELS[resource]);
            }
        }

        private string GetIndicatorSprite(ImmaterialResourceManager.Resource resource)
        {
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.HealthCare:
                    return "ToolbarIconHealthcare";
                case ImmaterialResourceManager.Resource.FireDepartment:
                    return "ToolbarIconFireDepartment";
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                    return "ToolbarIconPolice";
                case ImmaterialResourceManager.Resource.EducationElementary:
                    return "InfoIconEducation";
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                    return "InfoIconEducation";
                case ImmaterialResourceManager.Resource.EducationUniversity:
                    return "InfoIconEducation";
                case ImmaterialResourceManager.Resource.DeathCare:
                    return "NotificationIconDead";
                case ImmaterialResourceManager.Resource.PublicTransport:
                    return "InfoIconPublicTransport";
                case ImmaterialResourceManager.Resource.NoisePollution:
                    return "InfoIconNoisePollution";
                case ImmaterialResourceManager.Resource.CrimeRate:
                    return "InfoIconCrime";
                case ImmaterialResourceManager.Resource.Health:
                    return "InfoIconHealth";
                case ImmaterialResourceManager.Resource.Wellbeing:
                    return "InfoIconHappiness";
                case ImmaterialResourceManager.Resource.Density:
                    return "InfoIconPopulation";
                case ImmaterialResourceManager.Resource.Entertainment:
                    return "InfoIconEntertainment";
                case ImmaterialResourceManager.Resource.LandValue:
                    return "InfoIconLandValue";
                case ImmaterialResourceManager.Resource.Attractiveness:
                    return "InfoIconLevel";
                case ImmaterialResourceManager.Resource.Coverage:
                    return "ToolbarIconZoomOutGlobe";
                case ImmaterialResourceManager.Resource.FireHazard:
                    return "IconPolicySmokeDetectors";
                case ImmaterialResourceManager.Resource.Abandonment:
                    return "InfoIconDestruction";
                case ImmaterialResourceManager.Resource.CargoTransport:
                    return "InfoIconOutsideConnections";
                case ImmaterialResourceManager.Resource.RadioCoverage:
                    return "InfoIconRadio";
                case ImmaterialResourceManager.Resource.FirewatchCoverage:
                    return "InfoIconForestFire";
                case ImmaterialResourceManager.Resource.EarthquakeCoverage:
                    return "InfoIconEarthquake";
                case ImmaterialResourceManager.Resource.DisasterCoverage:
                    return "InfoIconDisasterDetection";
                case ImmaterialResourceManager.Resource.TourCoverage:
                    return "InfoIconTours";
                case ImmaterialResourceManager.Resource.PostService:
                    return "InfoIconPost";
                case ImmaterialResourceManager.Resource.EducationLibrary:
                    return "InfoIconEducation";
                case ImmaterialResourceManager.Resource.ChildCare:
                    return "InfoIconPopulation";
                case ImmaterialResourceManager.Resource.ElderCare:
                    return "InfoIconAge";
                case ImmaterialResourceManager.Resource.CashCollecting:
                    return "InfoIconFinancial";
                case ImmaterialResourceManager.Resource.TaxBonus:
                    return "InfoIconFinancial";
                case GroundPollution:
                    return "InfoIconGroundPollution"; // Infixo todo: does it exist?
                case ImmaterialResourceManager.Resource.None:
                    return "ToolbarIconHelp";
                default:
                    return "ToolbarIconHelp";
            }
        }

        private void GetIndicatorInfoModes(ImmaterialResourceManager.Resource resource, out InfoManager.InfoMode infoMode, out InfoManager.SubInfoMode subInfoMode)
        {
            infoMode = InfoManager.InfoMode.LandValue;
            subInfoMode = InfoManager.SubInfoMode.Default;

            switch (resource)
            {
                case ImmaterialResourceManager.Resource.HealthCare:
                    infoMode = InfoManager.InfoMode.Health;
                    subInfoMode = InfoManager.SubInfoMode.HealthCare;
                    break;
                case ImmaterialResourceManager.Resource.FireDepartment:
                    infoMode = InfoManager.InfoMode.FireSafety;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                    infoMode = InfoManager.InfoMode.CrimeRate;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.EducationElementary:
                    infoMode = InfoManager.InfoMode.Education;
                    subInfoMode = InfoManager.SubInfoMode.ElementarySchool;
                    break;
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                    infoMode = InfoManager.InfoMode.Education;
                    subInfoMode = InfoManager.SubInfoMode.HighSchool;
                    break;
                case ImmaterialResourceManager.Resource.EducationUniversity:
                    infoMode = InfoManager.InfoMode.Education;
                    subInfoMode = InfoManager.SubInfoMode.University;
                    break;
                case ImmaterialResourceManager.Resource.DeathCare:
                    infoMode = InfoManager.InfoMode.Health;
                    subInfoMode = InfoManager.SubInfoMode.DeathCare;
                    break;
                case ImmaterialResourceManager.Resource.PublicTransport:
                    infoMode = InfoManager.InfoMode.Transport;
                    subInfoMode = InfoManager.SubInfoMode.NormalTransport;
                    break;
                case ImmaterialResourceManager.Resource.NoisePollution:
                    infoMode = InfoManager.InfoMode.NoisePollution;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.CrimeRate:
                    infoMode = InfoManager.InfoMode.CrimeRate;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.Health:
                    infoMode = InfoManager.InfoMode.Health;
                    subInfoMode = InfoManager.SubInfoMode.HealthCare;
                    break;
                case ImmaterialResourceManager.Resource.Wellbeing:
                    infoMode = InfoManager.InfoMode.Happiness;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.Density:
                    infoMode = InfoManager.InfoMode.Density;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.Entertainment:
                    infoMode = InfoManager.InfoMode.Entertainment;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.LandValue:
                    infoMode = InfoManager.InfoMode.LandValue;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.Attractiveness:
                    infoMode = InfoManager.InfoMode.Tourism;
                    subInfoMode = InfoManager.SubInfoMode.Attractiveness;
                    break;
                case ImmaterialResourceManager.Resource.Coverage:
                    infoMode = InfoManager.InfoMode.LandValue;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.FireHazard:
                    infoMode = InfoManager.InfoMode.FireSafety;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.Abandonment:
                    infoMode = InfoManager.InfoMode.Destruction;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.CargoTransport:
                    infoMode = InfoManager.InfoMode.Connections;
                    subInfoMode = InfoManager.SubInfoMode.Import;
                    break;
                case ImmaterialResourceManager.Resource.RadioCoverage:
                    infoMode = InfoManager.InfoMode.Radio;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.FirewatchCoverage:
                    infoMode = InfoManager.InfoMode.FireSafety;
                    subInfoMode = InfoManager.SubInfoMode.ForestFireHazard;
                    break;
                case ImmaterialResourceManager.Resource.EarthquakeCoverage:
                    infoMode = InfoManager.InfoMode.DisasterDetection;
                    subInfoMode = InfoManager.SubInfoMode.EarthquakeDetection;
                    break;
                case ImmaterialResourceManager.Resource.DisasterCoverage:
                    infoMode = InfoManager.InfoMode.DisasterDetection;
                    subInfoMode = InfoManager.SubInfoMode.DisasterDetection;
                    break;
                case ImmaterialResourceManager.Resource.TourCoverage:
                    infoMode = InfoManager.InfoMode.Tours;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.PostService:
                    infoMode = InfoManager.InfoMode.Post;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                case ImmaterialResourceManager.Resource.EducationLibrary:
                    infoMode = InfoManager.InfoMode.Education;
                    subInfoMode = InfoManager.SubInfoMode.LibraryEducation;
                    break;
                case ImmaterialResourceManager.Resource.ChildCare:
                    infoMode = InfoManager.InfoMode.Health;
                    subInfoMode = InfoManager.SubInfoMode.ChildCare;
                    break;
                case ImmaterialResourceManager.Resource.ElderCare:
                    infoMode = InfoManager.InfoMode.Health;
                    subInfoMode = InfoManager.SubInfoMode.ElderCare;
                    break;
                case ImmaterialResourceManager.Resource.CashCollecting:
                    infoMode = InfoManager.InfoMode.Financial;
                    subInfoMode = InfoManager.SubInfoMode.FinancialBank;
                    break;
                case ImmaterialResourceManager.Resource.TaxBonus:
                    infoMode = InfoManager.InfoMode.Financial;
                    subInfoMode = InfoManager.SubInfoMode.FinancialStock;
                    break;
                case ImmaterialResourceManager.Resource.None:
                    infoMode = InfoManager.InfoMode.LandValue;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
                default:
                    infoMode = InfoManager.InfoMode.LandValue;
                    subInfoMode = InfoManager.SubInfoMode.Default;
                    break;
            }
        }

        // Sets top & botton icons accordingly to actual level progress and demand
        // The game is very buggy when both are set to 15!
        private void RefreshProgressStatusIcons(Zone zone, int topVal, int botVal)
        {
            float[] topMax = MAX_VALUES_TOPBAR[(int)zone];
            float[] botMax = MAX_VALUES_BOTBAR[(int)zone];
            // find top part and fraction
            int topLev = 0;
            float topFra = topVal / topMax[0];
            while (topVal >= topMax[topLev] && topLev < topMax.Length - 1)
            {
                topFra = (topVal - topMax[topLev]) / (topMax[topLev+1] - topMax[topLev]);
                topLev++;
            }
            // find botton part and fraction
            int botLev = 0;
            float botFra = botVal / botMax[0];
            while (botVal >= botMax[botLev] && botLev < botMax.Length - 1)
            {
                botFra = (botVal - botMax[botLev]) / (botMax[botLev+1] - botMax[botLev]);
                botLev++;
            }
            // where the progress is better?
            if (topLev == botLev)
            {
                // same - check fraction
                m_topBar.Happy = topFra > botFra;
                m_botBar.Happy = botFra > topFra;
            }
            else if (topLev > botLev)
            {
                m_topBar.Happy = true;
                m_botBar.Happy = false;
            }
            else
            {
                m_topBar.Happy = false;
                m_botBar.Happy = true;
            }
        }

        // The code is based on ResidentialBuildingAI.CheckBuildingLevel
        private void ShowResidentialProgress(ushort buildingId, ref Building building)
        {
            // extract progress info, reverse:
            // buildingData.m_levelUpProgress = (byte)(educationProgress | (landValueProgress << 4));
            int progressTop = building.m_levelUpProgress & 0xF; // ui-top is education
            int progressBot = building.m_levelUpProgress >> 4;  // ui-bottom is land value

            // education level needs to be re-calculated
            Citizen.BehaviourData behaviour = default;
            int aliveCount = 0;
            int totalCount = 0;
            int homeCount = 0;
            int aliveHomeCount = 0;
            int emptyHomeCount = 0;
            CommonBuildingAI buildingAI = (CommonBuildingAI)building.Info.m_buildingAI;
            CommonBuildingAI_Patches.GetHomeBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount, ref homeCount, ref aliveHomeCount, ref emptyHomeCount);

            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            int populace = behaviour.m_teenCount + behaviour.m_youngCount * 2 + behaviour.m_adultCount * 3 + behaviour.m_seniorCount * 3;
            if (populace != 0)
            {
                education = (education * 72 + (populace >> 1)) / populace;
            }

            // land value
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.LandValue, building.m_position, out var landValue);
            ShowLandValue(buildingId, ref building);

            // new shiny bars :)
            m_topBar.Text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            m_topBar.MaxValues = MAX_VALUES_TOPBAR[(int)Zone.R]; // new float[] { 15f, 30f, 45f, 60f, 80f };
            m_topBar.Value = education;
            m_topBar.Panel.tooltip = $"{progressTop} [Level 1]  ..14 | 15..29 | 30..44 | 45..59 | 60..  [Level 5]" + Environment.NewLine +
                $"Populace: Children {building.m_children} Teens {building.m_teens} Youngs {building.m_youngs} Adults {building.m_adults} Seniors {building.m_seniors}" + Environment.NewLine +
                $"Education: Edu {building.m_education1} Well {building.m_education2} High {building.m_education3}";
            m_botBar.Text = Locale.Get(LocaleID.INFO_LANDVALUE_TITLE);
            m_botBar.MaxValues = MAX_VALUES_BOTBAR[(int)Zone.R]; // new float[] { 6f, 21f, 41f, 61f, 80f };
            m_botBar.Value = landValue;
            m_botBar.Panel.tooltip = $"{progressBot} [Level 1]   ..5 |  6..20 | 21..40 | 41..60 | 61..  [Level 5]";
            RefreshProgressStatusIcons(Zone.R, education, landValue);
        }

        private int ProcessServiceValue(
            ushort[] resources, int index, // from CheckLocalResources
            ImmaterialResourceManager.Resource resourceType, // resource type
                                                             //int resourceRate, // not needed - taken from resources
            int middleRate, int maxRate, int middleEffect, int maxEffect, // params for CalculateResourceEffect
            int divisor, bool negative = false, int customRate = 0)
        {
            int resourceRate = ( (resourceType == GroundPollution || resourceType == WaterProximity) ? customRate : resources[index + (int)resourceType]);
            int value = ImmaterialResourceManager.CalculateResourceEffect(resourceRate, middleRate, maxRate, middleEffect, maxEffect);
            //int maxValue = maxEffect / divisor;
            UIServiceBar uiBar = m_uiServices[resourceType];
            uiBar.Negative = negative;
            uiBar.BelowMid = (resourceRate < middleRate);
            if (divisor < 10)
            {
                // Service coverage
                uiBar.ShowInts();
                uiBar.MaxValue = maxEffect / divisor;
                value /= divisor;
                uiBar.Value = value;
            }
            else
            {
                // Land value
                uiBar.ShowFloats();
                uiBar.MaxValue = (float)maxEffect / (float)divisor;
                uiBar.Value = (float)value / (float)divisor;
            }
            uiBar.Panel.tooltip = $"{resourceType}: rate={resourceRate} midMax={middleRate}->{maxRate} effect={middleEffect}->{maxEffect} div={divisor}";
            return value;
        }

        // This is an exact logical copy of IndustrialBuildingAI.CalculateServiceValue private method to get info about service coverage
        private int CalculateServiceValueIndustrial(ushort buildingID, ref Building data)
        {
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResources(data.m_position, out ushort[] resources, out var index);
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out var groundPollution);

            // new calculations
            int value = 0;
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PublicTransport, 100, 500, 50, 100, 3);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PoliceDepartment, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.HealthCare, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DeathCare, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PostService, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FireDepartment, 100, 500, 50, 100, 2);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Entertainment, 100, 500, 50, 100, 8);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationElementary, 100, 500, 50, 100, 8);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationHighSchool, 100, 500, 50, 100, 8);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationUniversity, 100, 500, 50, 100, 8);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.CargoTransport, 100, 500, 50, 100, 1);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.RadioCoverage, 50, 100, 80, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FirewatchCoverage, 100, 1000, 0, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DisasterCoverage, 50, 100, 80, 100, 5);
            // negatives
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.NoisePollution, 100, 500, 50, 100, 7, true);
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Abandonment, 100, 500, 50, 100, 7, true); // there is no UI overlay for that
            value -= ProcessServiceValue(resources, index, GroundPollution, 50, 255, 50, 100, 6, true, groundPollution); // special case

            return value;
        }

        private void ShowIndustrialProgress(ushort buildingId, ref Building building) // Infixo todo: Almost the same as Office but the factor is different, could be a param
        {
            // extract progress info, reverse:
            // buildingData.m_levelUpProgress = (byte)(educationProgress | (serviceProgress << 4));
            int progressTop = building.m_levelUpProgress & 0xF; // ui-top is education
            int progressBot = building.m_levelUpProgress >> 4;  // ui-bottom is service coverage

            // wealth level needs to be re-calculated; we need data about visitors for that
            Citizen.BehaviourData behaviour = default;
            int aliveWorkerCount = 0;
            int totalWorkerCount = 0;
            CommonBuildingAI buildingAI = (CommonBuildingAI)building.Info.m_buildingAI;
            CommonBuildingAI_Patches.GetWorkBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            if (aliveWorkerCount != 0)
            {
                education = (education * 20 + (aliveWorkerCount >> 1)) / aliveWorkerCount; // Infixo todo: it is 12 in Office
            }

            // service coverage
            m_uiServices[ImmaterialResourceManager.Resource.CargoTransport].Panel.Show();
            m_uiServices[ImmaterialResourceManager.Resource.EducationLibrary].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ChildCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ElderCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Health].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Wellbeing].Panel.Hide();
            m_uiServices[WaterProximity].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.FireHazard].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.CrimeRate].Panel.Hide();
            RescaleServiceBars(70f); // max is 50&100
            int service = CalculateServiceValueIndustrial(buildingId, ref building);

            // new shiny bars :)
            m_topBar.Text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            m_topBar.MaxValues = MAX_VALUES_TOPBAR[(int)Zone.I]; // new float[] { 15f, 30f, 45f };
            m_topBar.Value = education;
            m_topBar.Panel.tooltip = $"{progressTop} [Level 1]  ..14 | 15..29 | 30..  [Level 3]" + Environment.NewLine +
                $"Workers: {aliveWorkerCount}, Education: Edu {behaviour.m_educated1Count} Well {behaviour.m_educated2Count} High {behaviour.m_educated3Count}";
            m_botBar.Text = Locale.Get(LocaleID.POLICY_SERVICES);
            m_botBar.MaxValues = MAX_VALUES_BOTBAR[(int)Zone.I]; // new float[] { 30f, 60f, 90f };
            m_botBar.Value = service;
            m_botBar.Panel.tooltip = $"{progressBot} [Level 1]  ..29 | 30..59 | 60..  [Level 3]";
            RefreshProgressStatusIcons(Zone.I, education, service);
        }

        // The code is based on CommercialBuildingAI.CheckBuildingLevel
        private void ShowCommercialProgress(ushort buildingId, ref Building building)
        {
            // extract progress info, reverse:
            // buildingData.m_levelUpProgress = (byte)(wealthProgress | (landValueProgress << 4));
            int progressTop = building.m_levelUpProgress & 0xF; // ui-top is wealth
            int progressBot = building.m_levelUpProgress >> 4;  // ui-bottom is land value

            // wealth level needs to be re-calculated; we need data about visitors for that
            Citizen.BehaviourData behaviour = default(Citizen.BehaviourData);
            int aliveCount = 0;
            int totalCount = 0;
            CommonBuildingAI buildingAI = (CommonBuildingAI)building.Info.m_buildingAI;
            //if (ShowIt2Patcher.patched)
                CommonBuildingAI_Patches.GetVisitBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount); // aliveCount == visitorCount
            //else
                //GetVisitBehaviour(buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount); // aliveCount == visitorCount
            int wealth = behaviour.m_wealth1Count + behaviour.m_wealth2Count * 2 + behaviour.m_wealth3Count * 3;
            if (aliveCount != 0)
            {
                wealth = (wealth * 18 + (aliveCount >> 1)) / aliveCount;
            }

            // land value
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.LandValue, building.m_position, out var landValue);
            ShowLandValue(buildingId, ref building);

            // new shiny bars :)
            m_topBar.Text = "Wealth";
            m_topBar.MaxValues = MAX_VALUES_TOPBAR[(int)Zone.C]; // new float[] { 30f, 45f, 60f };
            m_topBar.Value = wealth;
            m_topBar.Panel.tooltip = $"{progressTop} [Level 1]  ..29 | 30..44 | 45..  [Level 3]" + Environment.NewLine +
                $"Visitors: {aliveCount}, Wealth: Low {behaviour.m_wealth1Count} Medium {behaviour.m_wealth2Count} High {behaviour.m_wealth3Count}";
            m_botBar.Text = Locale.Get(LocaleID.INFO_LANDVALUE_TITLE);
            m_botBar.MaxValues = MAX_VALUES_BOTBAR[(int)Zone.C]; // new float[] { 21f, 41f, 61f };
            m_botBar.Value = landValue;
            m_botBar.Panel.tooltip = $"{progressBot} [Level 1]  ..20 | 21..40 | 41..  [Level 3]";
            RefreshProgressStatusIcons(Zone.C, wealth, landValue);
        }

        // This is an exact logial copy of OfficeBuildingAI.CalculateServiceValue private method to get info about service coverage
        // Calls to CalculateResourceEffect are converted to ProcessServiceValue with exactly the same params
        private int CalculateServiceValueOffice(ushort buildingID, ref Building data)
        {
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResources(data.m_position, out var resources, out var index);
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out var groundPollution);

            // new calculations
            int value = 0;
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PublicTransport, 100, 500, 50, 100, 3);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PoliceDepartment, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.HealthCare, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DeathCare, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PostService, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FireDepartment, 100, 500, 50, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Entertainment, 100, 500, 50, 100, 6);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationElementary, 100, 500, 50, 100, 7);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationHighSchool, 100, 500, 50, 100, 7);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationUniversity, 100, 500, 50, 100, 7);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.RadioCoverage, 50, 100, 80, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FirewatchCoverage, 100, 1000, 0, 100, 5);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DisasterCoverage, 50, 100, 80, 100, 5);
            // negatives
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.NoisePollution, 100, 500, 50, 100, 4, true);
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Abandonment, 100, 500, 50, 100, 3, true); // there is no UI overlay for that
            value -= ProcessServiceValue(resources, index, GroundPollution, 50, 255, 50, 100, 4, true, groundPollution); // special case

            return value;
        }

        private void ShowOfficeProgress(ushort buildingID, ref Building building)
        {
            // extract progress info, reverse:
            // buildingData.m_levelUpProgress = (byte)(educationProgress | (serviceProgress << 4));
            int progressTop = building.m_levelUpProgress & 0xF; // ui-top is education
            int progressBot = building.m_levelUpProgress >> 4;  // ui-bottom is service coverage

            // wealth level needs to be re-calculated; we need data about visitors for that
            Citizen.BehaviourData behaviour = default;
            int aliveWorkerCount = 0;
            int totalWorkerCount = 0;
            CommonBuildingAI buildingAI = (CommonBuildingAI)building.Info.m_buildingAI;
            CommonBuildingAI_Patches.GetWorkBehaviour_Reverse(buildingAI, buildingID, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            if (aliveWorkerCount != 0)
            {
                education = (education * 12 + (aliveWorkerCount >> 1)) / aliveWorkerCount;
            }

            // service coverage
            m_uiServices[ImmaterialResourceManager.Resource.CargoTransport].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.EducationLibrary].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ChildCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ElderCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Health].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Wellbeing].Panel.Hide();
            m_uiServices[WaterProximity].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.FireHazard].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.CrimeRate].Panel.Hide();
            RescaleServiceBars(50f); // max is 33
            int service = CalculateServiceValueOffice(buildingID, ref building);

            // new shiny bars :)
            m_topBar.Text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            m_topBar.MaxValues = MAX_VALUES_TOPBAR[(int)Zone.O]; // new float[] { 15f, 30f, 45f };
            m_topBar.Value = education;
            m_topBar.Panel.tooltip = $"{progressTop} [Level 1]  ..14 | 15..29 | 30..  [Level 3]" + Environment.NewLine + // Infixo todo: change to slider
                $"Workers: {aliveWorkerCount}, Education: Edu {behaviour.m_educated1Count} Well {behaviour.m_educated2Count} High {behaviour.m_educated3Count}";
            m_botBar.Text = Locale.Get(LocaleID.POLICY_SERVICES);
            m_botBar.MaxValues = MAX_VALUES_BOTBAR[(int)Zone.O]; // new float[] { 45f, 90f, 135f };
            m_botBar.Value = service;
            m_botBar.Panel.tooltip = $"{progressBot} [Level 1]  ..44 | 45..89 | 90..  [Level 3]"; // Infixo todo: change to slider
            RefreshProgressStatusIcons(Zone.O, education, service);
        }

        // Health and Wellbeing have different formulas
        // If they are between 40 and 60 then their contribution is 0, below 40 is negative and above 60 is positive
        private int ProcessHealthWellbeing(
            ushort[] resources, int index, // from CheckLocalResources
            ImmaterialResourceManager.Resource resourceType,
            int middleRate, int maxRate, int middleEffect, int maxEffect) // params for CalculateResourceEffect
        {
            int resourceRate = resources[index + (int)resourceType];
            int valueAbove = ImmaterialResourceManager.CalculateResourceEffect(resourceRate, middleRate, maxRate, middleEffect, maxEffect);
            int valueBelow = ImmaterialResourceManager.CalculateResourceEffect(maxRate-resourceRate, middleRate, maxRate, middleEffect, maxEffect);
            UIServiceBar uiBar = m_uiServices[resourceType];
            bool isNeg = (resourceRate < maxRate / 2);
            uiBar.Negative = isNeg;
            uiBar.BelowMid = false; // below middle rate gives 0, so there will be no bar anyway...
            uiBar.ShowFloats();
            uiBar.MaxValue = (float)maxEffect / 10f; // always 10 for Land Value
            uiBar.Value = (isNeg ? (float)valueBelow / 10f : (float)valueAbove / 10f );
            uiBar.Panel.tooltip = $"{resourceType}: rate={resourceRate} midMax={middleRate}->{maxRate} effect={middleEffect}->{maxEffect} div=10";
            return isNeg ? -valueBelow : valueAbove;
        }

        private void ShowLandValue(ushort buildingID, ref Building data)
        {
            // UI adjustment
            foreach (UIServiceBar uiBar in m_uiServices.Values) // all are used
                uiBar.Panel.Show();
            RescaleServiceBars(20f); // max is 20

            Singleton<ImmaterialResourceManager>.instance.CheckLocalResources(data.m_position, out var resources, out var index);
            Singleton<NaturalResourceManager>.instance.CheckPollution(data.m_position, out var groundPollution);
            int realValue = resources[index + (int)ImmaterialResourceManager.Resource.LandValue];

            // new calculations
            int value = 0;
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.HealthCare, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FireDepartment, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PoliceDepartment, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationElementary, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationHighSchool, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationUniversity, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DeathCare, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PublicTransport, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.CargoTransport, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Entertainment, 100, 500, 100, 200, 10); // 20
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.RadioCoverage, 50, 100, 20, 25, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.DisasterCoverage, 50, 100, 20, 25, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FirewatchCoverage, 100, 1000, 0, 25, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.PostService, 100, 200, 20, 30, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.EducationLibrary, 100, 500, 50, 200, 10); // 20
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.ChildCare, 100, 500, 50, 100, 10);
            value += ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.ElderCare, 100, 500, 50, 100, 10);

            // negatives
            value -= ProcessServiceValue(resources, index, GroundPollution, 50, 255, 50, 100, 10, true, groundPollution); // special case
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.NoisePollution, 10, 100, 0, 100, 10, true);
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.CrimeRate, 10, 100, 0, 100, 10, true);
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.FireHazard, 50, 100, 10, 50, 10, true);
            value -= ProcessServiceValue(resources, index, ImmaterialResourceManager.Resource.Abandonment, 15, 50, 100, 200, 10, true); // there is no UI overlay for that, -20

            // health and wellbeing
            value += ProcessHealthWellbeing(resources, index, ImmaterialResourceManager.Resource.Health, 60, 100, 0, 50);
            value += ProcessHealthWellbeing(resources, index, ImmaterialResourceManager.Resource.Wellbeing, 60, 100, 0, 50);

            value /= 10;

            // water proximity
            // int num31 = CalculateResourceEffect(num29, 33, 67, 300, 0) * Mathf.Max(0, 32 - num28) >> 5; // LV
            // if pollution > 32 then 0, if < 32 then $$ += water * (pollution/32)
            // if water < 33 then proportional to 300, if > 33 then reverse proportional i.e. 0 -> 300 -> 0
            // ideal water adds +30 $$
            int waterValue = realValue - value;
            ProcessServiceValue(resources, index, WaterProximity, 150, 300, 150, 300, 10, false, waterValue*10); // output value is not needed here
            //Debug.Log($"ShowIt2.ShowLandValue: value={value} real={realValue} water={waterValue}");
        }
    }
}