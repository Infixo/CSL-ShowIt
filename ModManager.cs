using ColossalFramework;
using ColossalFramework.Globalization;
//using ColossalFramework.Math;
using ColossalFramework.UI;
//using ICities;
using System;
//using System.CodeDom;
//using System.Collections;
using System.Collections.Generic;
using System.Reflection;
//using System.Security.AccessControl;
using UnityEngine;
//using UnityEngine.Experimental.Director;
//using static Citizen;
//using static ColossalFramework.DataBinding.BindPropertyByKey;
//using static ImmaterialResourceManager;
//using static System.Net.Mime.MediaTypeNames;

// Alias the Enum: You can create an alias for an existing enum by using the using directive. 
using ImmaterialResource = ImmaterialResourceManager.Resource;

namespace ShowIt2
{

    public class ModManager : MonoBehaviour
    {
        public const ImmaterialResourceManager.Resource WaterProximity = (ImmaterialResourceManager.Resource)253; // Water proximity does not exists in IRM but it is used to calculate land value
        public const ImmaterialResourceManager.Resource GroundPollution = (ImmaterialResourceManager.Resource)254; // Ground Pollution does not exists in IRM but it is used to calculate service coverage value

        private bool _initialized;

        private ZonedBuildingWorldInfoPanel m_uiZonedBuildingWorldInfoPanel;
        private UIPanel _makeHistoricalPanel;
        private UICheckBox _indicatorsCheckBox;
        private UIPanel m_uiMainPanel;
        // leveling progress - wealth, education section
        private UILabel _progTopName; // education or wealth
        private UILabel _progTopProgress; // number 1..15
        private UILabel _progTopValue; // actual value number
        // Infixo: todo slider showing nicely the progress and tresholds for various levels
        // leveling progress - land value, service coverage
        private UILabel _progBotName; // land value or service coverage
        private UILabel _progBotProgress; // number 1..15
        private UILabel _progBotValue; // actual value number
        // Infixo: todo slider showing nicely the progress and tresholds for various levels

        // service coverage
        //private UILabel _header;
        private Dictionary<int, UIRadialChart> _charts;
        private Dictionary<int, UILabel> _numbers;
        private Dictionary<int, UILabel> _maxVals; // Infixo: a set of labels showing max indicator values
        private Dictionary<int, UISprite> _icons;
        private Dictionary<int, UILabel> _labels;
        private Dictionary<ImmaterialResourceManager.Resource, UIServiceBar> m_uiServices;

        //private ushort _cachedBuildingID;
        private Dictionary<int, float> _effectsOnZonedBuilding;
        private Dictionary<int, float> _maxEffectsOnZonedBuilding;

        private const int MaxNumberOfCharts = 17;

        public void Awake()
        {
            try
            {
                _charts = new Dictionary<int, UIRadialChart>();
                _numbers = new Dictionary<int, UILabel>();
                _maxVals = new Dictionary<int, UILabel>();
                _icons = new Dictionary<int, UISprite>();
                _labels = new Dictionary<int, UILabel>();
                m_uiServices = new Dictionary<ImmaterialResourceManager.Resource, UIServiceBar>();

                _effectsOnZonedBuilding = new Dictionary<int, float>();
                _maxEffectsOnZonedBuilding = new Dictionary<int, float>();
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:Awake -> Exception: " + e.Message);
            }
        }

        public void Start()
        {
            try
            {
                m_uiZonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
                _makeHistoricalPanel = m_uiZonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();

                CreateUI();
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:Start -> Exception: " + e.Message);
            }
        }

        public void OnSetTarget(ushort buildingID)
        {
            ushort hackedID = ((InstanceID)m_uiZonedBuildingWorldInfoPanel
                    .GetType()
                    .GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(m_uiZonedBuildingWorldInfoPanel))
                    .Building;
            Debug.Log($"ShowIt2.ModManager.OnSetTarget: passed id={buildingID} hacked id={hackedID}");
            if (m_uiMainPanel.isVisible)
                RefreshData();
        }

        public void Update()
        {
            //try
            //{
            if (!_initialized || ModConfig.Instance.ConfigUpdated)
            {
                UpdateUI();

                _initialized = true;
                ModConfig.Instance.ConfigUpdated = false;
            }

            //if (m_uiZonedBuildingWorldInfoPanel.component.isVisible && m_uiMainPanel.isVisible)
            //{
            //_cachedBuildingID = 0;
            //}
            //else
            //{
            //RefreshData();
            //}
            //}
            //catch (Exception e)
            //{
            //Debug.Log("[Show It!] ModManager:Update -> Exception: " + e.Message);
            //}
        }

        public void OnDestroy()
        {
            //try
            //{
            /*
            for (var i = 0; i < MaxNumberOfCharts; i++)
            {
                Destroy(_charts[i]);
                Destroy(_numbers[i]);
                Destroy(_maxVals[i]); // Infixo
                Destroy(_icons[i]);
                Destroy(_labels[i]);
            }
            */
            //if (_header != null)
            //{
            //Destroy(_header);
            //}
            if (m_uiMainPanel != null)
            {
                Destroy(m_uiMainPanel);
            }
            if (_indicatorsCheckBox != null)
            {
                Destroy(_indicatorsCheckBox);
            }
            // Infixo todo: add Destroy for other labels
            // Infixo todo: destroy service bars
            //}
            //catch (Exception e)
            //{
            //Debug.Log("[Show It!] ModManager:OnDestroy -> Exception: " + e.Message);
            //}
        }

        private void CreateUIServiceBar(UIComponent parent, int position, ImmaterialResourceManager.Resource resource)
        {
            UIServiceBar uiServiceBar = new UIServiceBar(parent, "ShowIt2" + resource.ToString() + "ServiceBar");
            //parent.AttachUIComponent(uiServiceBar);
            //BudgetItem budgetItem = uIComponent.GetComponent<BudgetItem>();
            //UIServiceBar uiServiceBar = parent.GetComponent<UIServiceBar>();
            uiServiceBar.Width = 440f; // width of the parent is 0 atm
            uiServiceBar.Panel.relativePosition = new Vector3(10f, 60f + position * (UIServiceBar.DEFAULT_HEIGHT + 2f)); // Infixo todo: scaling
            //uiServiceBar.Text = GetResourceName(resource);
            uiServiceBar.Text = GetResourceName(resource);
            uiServiceBar.Limit = 60f; // Biggest is Cargo 100, then Fire 50, others are <= 33
            uiServiceBar.MaxValue = 20f;
            m_uiServices.Add(resource, uiServiceBar);
            // disable the ones not available
            GetIndicatorInfoModes(resource, out InfoManager.InfoMode infoMode, out InfoManager.SubInfoMode subInfoMode);
            if (resource != GroundPollution && resource != ImmaterialResourceManager.Resource.Abandonment) // there is no UI overlay for Abandonment
                uiServiceBar.Panel.isEnabled = Singleton<InfoManager>.instance.IsInfoModeAvailable(infoMode);
        }

        private void RescaleServiceBars(float limit)
        {
            foreach (UIServiceBar uiBar in m_uiServices.Values)
                uiBar.Limit = limit;
        }

        private void CreateUI()
        {
            //try
            //{
            m_uiMainPanel = m_uiZonedBuildingWorldInfoPanel.component.AddUIComponent<UIPanel>();
            m_uiMainPanel.name = "ShowIt2Panel";

            m_uiMainPanel.backgroundSprite = "SubcategoriesPanel";
            m_uiMainPanel.opacity = 0.90f;
            m_uiMainPanel.height = 60f + (UIServiceBar.DEFAULT_HEIGHT + 2f) * 24 + 10f;
            m_uiMainPanel.width = m_uiZonedBuildingWorldInfoPanel.component.width;

            _indicatorsCheckBox = UIUtils.CreateCheckBox(_makeHistoricalPanel, "ShowItIndicatorsCheckBox", "Indicators", ModConfig.Instance.ShowPanel);
            _indicatorsCheckBox.width = 110f;
            _indicatorsCheckBox.label.textColor = new Color32(185, 221, 254, 255);
            _indicatorsCheckBox.label.textScale = 0.8125f;
            _indicatorsCheckBox.tooltip = "Indicators will show how well serviced the building is and what problems might prevent the building from leveling up.";
            _indicatorsCheckBox.AlignTo(_makeHistoricalPanel, UIAlignAnchor.TopLeft);
            _indicatorsCheckBox.relativePosition = new Vector3(_makeHistoricalPanel.width - _indicatorsCheckBox.width, 6f);
            _indicatorsCheckBox.eventCheckChanged += (component, value) =>
            {
                m_uiMainPanel.isVisible = value;
                ModConfig.Instance.ShowPanel = value;
                ModConfig.Instance.Save();
            };

            //_header = UIUtils.CreateLabel(m_uiMainPanel, "ShowItIndicatorsPanelHeader", "Service coverage");
            //_header.font = UIUtils.GetUIFont("OpenSans-Regular");
            //_header.textAlignment = UIHorizontalAlignment.Center;

            // Infixo: new section showing leveling progress
            // leveling progress - wealth, education section
            _progTopName = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgTopName", "Education/Wealth"); // education or wealth
            _progTopProgress = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgTopProgress", "-1"); // number 1..15
            _progTopValue = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgTopValue", "-1"); // actual value number
            _progTopName.relativePosition = new Vector3(10, 10);// education or wealth
            _progTopProgress.relativePosition = new Vector3(150, 10); // number 1..15
            _progTopValue.relativePosition = new Vector3(200, 10); // actual value number
            // Infixo: todo slider showing nicely the progress and tresholds for various levels

            // leveling progress - land value, service coverage
            _progBotName = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgBotName", "Land value/Service coverage"); // education or wealth
            _progBotProgress = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgBotProgress", "-1"); // number 1..15
            _progBotValue = UIUtils.CreateLabel(m_uiMainPanel, "ShowItPanelProgBotValue", "-1"); // actual value number
            _progBotName.relativePosition = new Vector3(10, 30); // land value or service coverage
            _progBotProgress.relativePosition = new Vector3(150, 30); // number 1..15
            _progBotValue.relativePosition = new Vector3(200, 30); // actual value number
            // Infixo: todo slider showing nicely the progress and tresholds for various levels


            /*
            UIRadialChart chart; // Infixo: radial chart -> change into a slider whowing max value
            UILabel number; // indicator value
            UILabel maxVal; // indicator max value (a little smaller)
            UISprite icon;
            UILabel label; // indicator name

            for (var i = 0; i < MaxNumberOfCharts; i++)
            {
                chart = UIUtils.CreateTwoSlicedRadialChart(m_uiMainPanel, "ShowItZonedIndicatorsPanelChart" + i);
                chart.eventClick += (component, eventParam) =>
                {
                    InfoManager.InfoMode infoMode = InfoManager.InfoMode.LandValue;
                    InfoManager.SubInfoMode subInfoMode = InfoManager.SubInfoMode.Default;

                    GetIndicatorInfoModes((ImmaterialResourceManager.Resource)component.objectUserData, out infoMode, out subInfoMode);

                    if (Singleton<InfoManager>.instance.IsInfoModeAvailable(infoMode))
                    {
                        Singleton<InfoManager>.instance.SetCurrentMode(infoMode, subInfoMode);
                    }
                };
                _charts.Add(i, chart);

                number = UIUtils.CreateLabel(chart, "ShowItIndicatorsPanelNumber" + i, "");
                //number.textAlignment = UIHorizontalAlignment.Center;
                _numbers.Add(i, number);

                maxVal = UIUtils.CreateLabel(chart, "ShowItIndicatorsPanelNumber" + i, "");
                //maxVal.textAlignment = UIHorizontalAlignment.Center;
                _maxVals.Add(i, maxVal);

                icon = UIUtils.CreateSprite(chart, "ShowItIndicatorsPanelIcon" + i, "");
                _icons.Add(i, icon);

                label = UIUtils.CreateLabel(chart, "ShowItIndicatorsPanelLabel" + i, "");
                label.font = UIUtils.GetUIFont("OpenSans-Regular");
                //label.textAlignment = UIHorizontalAlignment.Center;
                label.textColor = new Color32(206, 248, 0, 255);
                _labels.Add(i, label);
            }
            */
            // service bars
            // UIComponent tree
            // ShowItIndicatorsPanel (UIPanel)-> attached via AddComponent<>
            // ...singular controls
            // ...ShowItXXXServiceBar (UIPanel)
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
            // negatives
            CreateUIServiceBar(m_uiMainPanel, 19, ImmaterialResourceManager.Resource.Abandonment);
            CreateUIServiceBar(m_uiMainPanel, 20, ImmaterialResourceManager.Resource.NoisePollution);
            CreateUIServiceBar(m_uiMainPanel, 21, GroundPollution);
            CreateUIServiceBar(m_uiMainPanel, 22, ImmaterialResourceManager.Resource.FireHazard); // land value
            CreateUIServiceBar(m_uiMainPanel, 23, ImmaterialResourceManager.Resource.CrimeRate); // land value
                                                                                                 //}
                                                                                                 //catch (Exception e)
                                                                                                 //{
                                                                                                 //Debug.Log("[Show It!] ModManager:CreateUI -> Exception: " + e.Message);
                                                                                                 //}
        }

        // Infixo: place UI controls and reorganize UI when config changed
        private void UpdateUI()
        {
            //try
            //{
            // Infixo: progress section
            //_header.relativePosition = new Vector3(m_uiMainPanel.width / 2f - _header.width / 2f, _header.height / 2f + 5f);
            // Infixo: new section showing leveling progress
            // leveling progress - wealth, education section
            _progTopName.relativePosition = new Vector3(10, 10);// education or wealth
            _progTopProgress.relativePosition = new Vector3(150, 10); // number 1..15
            _progTopValue.relativePosition = new Vector3(200, 10); // actual value number
                                                                   // Infixo: todo slider showing nicely the progress and tresholds for various levels
                                                                   // leveling progress - land value, service coverage
            _progBotName.relativePosition = new Vector3(10, 30); // land value or service coverage
            _progBotProgress.relativePosition = new Vector3(150, 30); // number 1..15
            _progBotValue.relativePosition = new Vector3(200, 30); // actual value number
                                                                   // Infixo: todo slider showing nicely the progress and tresholds for various levels

            //int rows;
            //int columns;
            //float horizontalSpacing = ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing;
            float verticalSpacing = ModConfig.Instance.Spacing;
            //float topSpacing = 40f; // Infixo: move all controls 300 down 60f + position * (UIServiceBar.DEFAULT_HEIGHT + 2f)
            //float bottomSpacing = 15f;
            //float horizontalPadding = 0f;
            //float verticalPadding = 0f;

            if (ModConfig.Instance.Alignment is "Right")
            {
                //rows = Mathf.FloorToInt((m_uiMainPanel.parent.height - topSpacing - bottomSpacing - verticalSpacing) / (ModConfig.Instance.Scaling + ModConfig.Instance.Spacing));
                //columns = Mathf.CeilToInt((float)MaxNumberOfCharts / rows);

                m_uiMainPanel.AlignTo(m_uiMainPanel.parent, UIAlignAnchor.TopRight);
                //m_uiMainPanel.width = columns * (ModConfig.Instance.Scaling + ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing);
                //m_uiMainPanel.height = m_uiMainPanel.parent.height - bottomSpacing;
                m_uiMainPanel.relativePosition = new Vector3(m_uiMainPanel.parent.width + 1f, 0f);

                //horizontalPadding = ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing / 2f;
                //verticalPadding = (m_uiMainPanel.parent.height - topSpacing - bottomSpacing - rows * (ModConfig.Instance.Scaling + ModConfig.Instance.Spacing)) / 2f;
            }
            else
            {
                //columns = Mathf.FloorToInt((m_uiMainPanel.parent.width - horizontalSpacing) / (ModConfig.Instance.Scaling + ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing));
                //rows = Mathf.CeilToInt((float)MaxNumberOfCharts / columns);

                m_uiMainPanel.AlignTo(m_uiMainPanel.parent, UIAlignAnchor.BottomLeft);
                //m_uiMainPanel.width = m_uiMainPanel.parent.width;
                //m_uiMainPanel.height = rows * (ModConfig.Instance.Scaling + ModConfig.Instance.Spacing) + topSpacing + bottomSpacing;
                m_uiMainPanel.relativePosition = new Vector3(0f, m_uiMainPanel.parent.height + 1f);

                //horizontalPadding = (m_uiMainPanel.parent.width - columns * (ModConfig.Instance.Scaling + ModConfig.Instance.IndicatorsPanelChartHorizontalSpacing)) / 2f;
                //verticalPadding = ModConfig.Instance.Spacing / 2f;
            }
            // Infixo todo: FIX change later!!!!
            //m_uiMainPanel.height = m_uiMainPanel.height + 25f * 17;

            //_header.relativePosition = new Vector3(m_uiMainPanel.width / 2f - _header.width / 2f, _header.height / 2f + 105f); // Infixo: move down by 100
            /*
            UIRadialChart chart;

            for (var i = 0; i < MaxNumberOfCharts; i++)
            {
                chart = _charts[i];
                chart.AlignTo(m_uiMainPanel, UIAlignAnchor.TopRight);
                chart.size = new Vector3(ModConfig.Instance.Scaling, ModConfig.Instance.Scaling);
                chart.relativePosition = new Vector3(horizontalPadding + i % columns * (chart.width + horizontalSpacing), topSpacing + verticalPadding + i / columns * (chart.height + verticalSpacing));

                _numbers[i].textScale = ModConfig.Instance.IndicatorsPanelNumberTextScale;
                _maxVals[i].textScale = ModConfig.Instance.IndicatorsPanelNumberTextScale;
                _icons[i].size = new Vector3(ModConfig.Instance.IndicatorsPanelIconSize, ModConfig.Instance.IndicatorsPanelIconSize);
                _labels[i].textScale = ModConfig.Instance.IndicatorsPanelLabelTextScale;
            }
            */
            //}
            //catch (Exception e)
            //{
            //Debug.Log("[Show It!] ModManager:UpdateUI -> Exception: " + e.Message);
            //}
        }

        private void SetCharts(ImmaterialResourceManager.Resource[] resources)
        {
            try
            {
                int chartIndex = 0;

                for (var i = 0; i < resources.Length; i++)
                {
                    if (IsIndicatorAvailable(resources[i]))
                    {
                        //SetChart(chartIndex, resources[i]);
                        chartIndex++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:SetCharts -> Exception: " + e.Message);
            }
        }

        private void SetChart(int index, ImmaterialResourceManager.Resource resource)
        {
            try
            {
                int resourceKey = (int)resource;
                float resourceEffect = _effectsOnZonedBuilding[resourceKey];
                float resourceMaxEffect = _maxEffectsOnZonedBuilding[resourceKey];
                float resourceEffectPercentage = resourceEffect / resourceMaxEffect;

                Color32 colorRed = new Color32(241, 136, 136, 255);
                Color32 colorYellow = new Color32(251, 212, 0, 255);
                Color32 colorDarkGreen = new Color32(141, 149, 55, 255);
                Color32 colorLightGreen = new Color32(131, 213, 141, 255);
                Color32 color;

                if (resourceEffectPercentage > 0.75f)
                {
                    color = IsIndicatorPositive(resource) ? colorLightGreen : colorRed;
                }
                else if (resourceEffectPercentage > 0.50f)
                {
                    color = IsIndicatorPositive(resource) ? colorDarkGreen : colorRed;
                }
                else if (resourceEffectPercentage > 0.25f)
                {
                    color = IsIndicatorPositive(resource) ? colorYellow : colorRed;
                }
                else
                {
                    color = IsIndicatorPositive(resource) ? colorRed : colorRed;
                }

                _charts[index].GetSlice(0).outterColor = color;
                _charts[index].GetSlice(0).innerColor = color;

                _charts[index].SetValues(resourceEffectPercentage, 1 - resourceEffectPercentage);
                _charts[index].tooltip = GetResourceName(resource);
                _charts[index].objectUserData = resource;
                _charts[index].isVisible = true;

                _numbers[index].text = $"{Math.Round(resourceEffectPercentage * 100f),1}%";
                _numbers[index].relativePosition = new Vector3(_charts[index].width / 2f - _numbers[index].width / 2f, _charts[index].height / 2f - _numbers[index].height / 2f - 10); // Infixo todo: better placing
                _numbers[index].isVisible = true;

                _maxVals[index].text = "0"; // Infixo todo?
                _maxVals[index].relativePosition = new Vector3(_charts[index].width / 2f - _numbers[index].width / 2f, _charts[index].height / 2f - _numbers[index].height / 2f + 10);
                _maxVals[index].isVisible = true;

                _icons[index].spriteName = GetIndicatorSprite(resource);
                _icons[index].tooltip = GetResourceName(resource);

                _labels[index].text = GetResourceName(resource).Substring(0, 4) + ".";
                _labels[index].tooltip = GetResourceName(resource);

                //if (ModConfig.Instance.IndicatorsPanelLegend is "Icons")
                //{
                //_icons[index].position = new Vector3(_charts[index].width / 2f - _icons[index].width / 2f, 0f - (ModConfig.Instance.Scaling / 1.25f));
                //_icons[index].isVisible = true;
                //}
                //else if (ModConfig.Instance.IndicatorsPanelLegend is "Labels")
                //{
                //_labels[index].position = new Vector3(_charts[index].width / 2f - _labels[index].width / 2f, 0f - ModConfig.Instance.Scaling);
                //_labels[index].isVisible = true;
                //}
                //else
                //{
                //_icons[index].position = new Vector3(_charts[index].width / 2f - _icons[index].width / 2f, 0f - (ModConfig.Instance.Scaling / 1.75f));
                //_labels[index].position = new Vector3(_charts[index].width / 2f - _labels[index].width / 2f, 0f - ModConfig.Instance.Scaling);
                //_icons[index].isVisible = true;
                //_labels[index].isVisible = true;
                //}
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:SetChart -> Exception: " + e.Message);
            }
        }

        private void ResetAllCharts()
        {
            try
            {
                foreach (UIRadialChart chart in _charts.Values)
                {
                    chart.isVisible = false;
                }

                foreach (UILabel number in _numbers.Values)
                {
                    number.isVisible = false;
                }

                foreach (UILabel maxVal in _maxVals.Values)
                {
                    maxVal.isVisible = false;
                }

                foreach (UISprite icon in _icons.Values)
                {
                    icon.isVisible = false;
                }

                foreach (UILabel label in _labels.Values)
                {
                    label.isVisible = false;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:ResetAllCharts -> Exception: " + e.Message);
            }
        }

        private void RefreshData()
        {
            //try
            //{
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            //ushort buildingId = ((InstanceID)m_uiZonedBuildingWorldInfoPanel
            //.GetType()
            //.GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance)
            //.GetValue(m_uiZonedBuildingWorldInfoPanel))
            //.Building;

            //if (_cachedBuildingID == 0 || _cachedBuildingID != buildingID)
            //{
            Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            /*
                    _effectsOnZonedBuilding.Clear();
                    _maxEffectsOnZonedBuilding.Clear();

                    float effect;
                    float maxEffect;

                    for (var i = 0; i < ImmaterialResourceManager.RESOURCE_COUNT; i++)
                    {
                        switch (building.Info.m_class.GetZone())
                        {
                            case ItemClass.Zone.ResidentialHigh:
                            case ItemClass.Zone.ResidentialLow:
                                effect = ResidentialBuildingHelper.CalculateResourceEffect(buildingID, ref building, (ImmaterialResourceManager.Resource)i);
                                maxEffect = ResidentialBuildingHelper.GetMaxEffect((ImmaterialResourceManager.Resource)i);
                                break;
                            case ItemClass.Zone.Industrial:
                                effect = IndustrialBuildingHelper.CalculateResourceEffect(buildingID, ref building, (ImmaterialResourceManager.Resource)i);
                                maxEffect = IndustrialBuildingHelper.GetMaxEffect((ImmaterialResourceManager.Resource)i);
                                break;
                            case ItemClass.Zone.CommercialHigh:
                            case ItemClass.Zone.CommercialLow:
                                effect = CommercialBuildingHelper.CalculateResourceEffect(buildingID, ref building, (ImmaterialResourceManager.Resource)i);
                                maxEffect = CommercialBuildingHelper.GetMaxEffect((ImmaterialResourceManager.Resource)i);
                                break;
                            case ItemClass.Zone.Office:
                                effect = OfficeBuildingHelper.CalculateResourceEffect(buildingID, ref building, (ImmaterialResourceManager.Resource)i);
                                maxEffect = OfficeBuildingHelper.GetMaxEffect((ImmaterialResourceManager.Resource)i);
                                break;
                            default:
                                effect = 0;
                                maxEffect = 0;
                                break;
                        }

                        _effectsOnZonedBuilding.Add(i, effect);
                        _maxEffectsOnZonedBuilding.Add(i, maxEffect);
                    }

                    //ResetAllCharts();
            */
            switch (building.Info.m_class.GetZone())
            {
                case ItemClass.Zone.ResidentialHigh:
                case ItemClass.Zone.ResidentialLow:
                    //ShowResidentialCharts();
                    ShowResidentialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.Industrial:
                    //ShowIndustrialCharts();
                    ShowIndustrialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.CommercialHigh:
                case ItemClass.Zone.CommercialLow:
                    //ShowCommercialCharts();
                    ShowCommercialProgress(buildingID, ref building);
                    break;
                case ItemClass.Zone.Office:
                    //ShowOfficeCharts();
                    ShowOfficeProgress(buildingID, ref building);
                    break;
                default:

                    break;
            }

            //_cachedBuildingID = buildingID;
            //}
            //}
            //catch (Exception e)
            //{
            //Debug.Log("[Show It!] ModManager:RefreshData -> Exception: " + e.Message);
            //}
        }

        private void ShowResidentialCharts()
        {
            try
            {
                ImmaterialResourceManager.Resource[] resources = new ImmaterialResourceManager.Resource[15];

                resources[0] = ImmaterialResourceManager.Resource.HealthCare;
                resources[1] = ImmaterialResourceManager.Resource.DeathCare;
                resources[2] = ImmaterialResourceManager.Resource.FireDepartment;
                resources[3] = ImmaterialResourceManager.Resource.PoliceDepartment;
                resources[4] = ImmaterialResourceManager.Resource.EducationElementary;
                resources[5] = ImmaterialResourceManager.Resource.EducationHighSchool;
                resources[6] = ImmaterialResourceManager.Resource.EducationUniversity;
                resources[7] = ImmaterialResourceManager.Resource.PublicTransport;
                resources[8] = ImmaterialResourceManager.Resource.PostService;
                resources[9] = ImmaterialResourceManager.Resource.Entertainment;
                resources[10] = ImmaterialResourceManager.Resource.FirewatchCoverage;
                resources[11] = ImmaterialResourceManager.Resource.DisasterCoverage;
                resources[12] = ImmaterialResourceManager.Resource.RadioCoverage;
                resources[13] = ImmaterialResourceManager.Resource.NoisePollution;
                resources[14] = ImmaterialResourceManager.Resource.Abandonment;

                SetCharts(resources);
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:ShowResidentialCharts -> Exception: " + e.Message);
            }
        }

        private void ShowIndustrialCharts()
        {
            try
            {
                ImmaterialResourceManager.Resource[] resources = new ImmaterialResourceManager.Resource[16];

                resources[0] = ImmaterialResourceManager.Resource.HealthCare;
                resources[1] = ImmaterialResourceManager.Resource.DeathCare;
                resources[2] = ImmaterialResourceManager.Resource.FireDepartment;
                resources[3] = ImmaterialResourceManager.Resource.PoliceDepartment;
                resources[4] = ImmaterialResourceManager.Resource.EducationElementary;
                resources[5] = ImmaterialResourceManager.Resource.EducationHighSchool;
                resources[6] = ImmaterialResourceManager.Resource.EducationUniversity;
                resources[7] = ImmaterialResourceManager.Resource.PublicTransport;
                resources[8] = ImmaterialResourceManager.Resource.PostService;
                resources[9] = ImmaterialResourceManager.Resource.Entertainment;
                resources[10] = ImmaterialResourceManager.Resource.FirewatchCoverage;
                resources[11] = ImmaterialResourceManager.Resource.DisasterCoverage;
                resources[12] = ImmaterialResourceManager.Resource.RadioCoverage;
                resources[13] = ImmaterialResourceManager.Resource.CargoTransport;
                resources[14] = ImmaterialResourceManager.Resource.NoisePollution;
                resources[15] = ImmaterialResourceManager.Resource.Abandonment;

                SetCharts(resources);
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:ShowIndustrialCharts -> Exception: " + e.Message);
            }
        }

        private void ShowCommercialCharts()
        {
            try
            {
                ImmaterialResourceManager.Resource[] resources = new ImmaterialResourceManager.Resource[17];

                resources[0] = ImmaterialResourceManager.Resource.HealthCare;
                resources[1] = ImmaterialResourceManager.Resource.DeathCare;
                resources[2] = ImmaterialResourceManager.Resource.FireDepartment;
                resources[3] = ImmaterialResourceManager.Resource.PoliceDepartment;
                resources[4] = ImmaterialResourceManager.Resource.EducationElementary;
                resources[5] = ImmaterialResourceManager.Resource.EducationHighSchool;
                resources[6] = ImmaterialResourceManager.Resource.EducationUniversity;
                resources[7] = ImmaterialResourceManager.Resource.PublicTransport;
                resources[8] = ImmaterialResourceManager.Resource.PostService;
                resources[9] = ImmaterialResourceManager.Resource.Entertainment;
                resources[10] = ImmaterialResourceManager.Resource.FirewatchCoverage;
                resources[11] = ImmaterialResourceManager.Resource.DisasterCoverage;
                resources[12] = ImmaterialResourceManager.Resource.RadioCoverage;
                resources[13] = ImmaterialResourceManager.Resource.CargoTransport;
                resources[14] = ImmaterialResourceManager.Resource.NoisePollution;
                resources[15] = ImmaterialResourceManager.Resource.Abandonment;
                resources[16] = ImmaterialResourceManager.Resource.CashCollecting;

                SetCharts(resources);
            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:ShowCommercialCharts -> Exception: " + e.Message);
            }
        }

        private void ShowOfficeCharts()
        {
            try
            {
                ImmaterialResourceManager.Resource[] resources = new ImmaterialResourceManager.Resource[15];

                resources[0] = ImmaterialResourceManager.Resource.HealthCare;
                resources[1] = ImmaterialResourceManager.Resource.DeathCare;
                resources[2] = ImmaterialResourceManager.Resource.FireDepartment;
                resources[3] = ImmaterialResourceManager.Resource.PoliceDepartment;
                resources[4] = ImmaterialResourceManager.Resource.EducationElementary;
                resources[5] = ImmaterialResourceManager.Resource.EducationHighSchool;
                resources[6] = ImmaterialResourceManager.Resource.EducationUniversity;
                resources[7] = ImmaterialResourceManager.Resource.PublicTransport;
                resources[8] = ImmaterialResourceManager.Resource.PostService;
                resources[9] = ImmaterialResourceManager.Resource.Entertainment;
                resources[10] = ImmaterialResourceManager.Resource.FirewatchCoverage;
                resources[11] = ImmaterialResourceManager.Resource.DisasterCoverage;
                resources[12] = ImmaterialResourceManager.Resource.RadioCoverage;
                resources[13] = ImmaterialResourceManager.Resource.NoisePollution;
                resources[14] = ImmaterialResourceManager.Resource.Abandonment;

                SetCharts(resources);

                // Infixo new calculations


            }
            catch (Exception e)
            {
                Debug.Log("[Show It!] ModManager:ShowOfficeCharts -> Exception: " + e.Message);
            }
        }

        private bool IsIndicatorAvailable(ImmaterialResourceManager.Resource resource)
        {
            InfoManager.InfoMode infoMode = InfoManager.InfoMode.LandValue;
            InfoManager.SubInfoMode subInfoMode = InfoManager.SubInfoMode.Default;

            GetIndicatorInfoModes(resource, out infoMode, out subInfoMode);

            return Singleton<InfoManager>.instance.IsInfoModeAvailable(infoMode);

        }

        private bool IsIndicatorPositive(ImmaterialResourceManager.Resource resource)
        {
            switch (resource)
            {
                case ImmaterialResourceManager.Resource.HealthCare:
                    return true;
                case ImmaterialResourceManager.Resource.FireDepartment:
                    return true;
                case ImmaterialResourceManager.Resource.PoliceDepartment:
                    return true;
                case ImmaterialResourceManager.Resource.EducationElementary:
                    return true;
                case ImmaterialResourceManager.Resource.EducationHighSchool:
                    return true;
                case ImmaterialResourceManager.Resource.EducationUniversity:
                    return true;
                case ImmaterialResourceManager.Resource.EducationLibrary:
                    return true;
                case ImmaterialResourceManager.Resource.DeathCare:
                    return true;
                case ImmaterialResourceManager.Resource.PublicTransport:
                    return true;
                case ImmaterialResourceManager.Resource.NoisePollution:
                    return false;
                case ImmaterialResourceManager.Resource.CrimeRate:
                    return false;
                case ImmaterialResourceManager.Resource.Health:
                    return true;
                case ImmaterialResourceManager.Resource.Wellbeing:
                    return true;
                case ImmaterialResourceManager.Resource.Density:
                    return true;
                case ImmaterialResourceManager.Resource.Entertainment:
                    return true;
                case ImmaterialResourceManager.Resource.LandValue:
                    return true;
                case ImmaterialResourceManager.Resource.Attractiveness:
                    return true;
                case ImmaterialResourceManager.Resource.Coverage:
                    return true;
                case ImmaterialResourceManager.Resource.FireHazard:
                    return false;
                case ImmaterialResourceManager.Resource.Abandonment:
                    return false;
                case ImmaterialResourceManager.Resource.CargoTransport:
                    return true;
                case ImmaterialResourceManager.Resource.RadioCoverage:
                    return true;
                case ImmaterialResourceManager.Resource.FirewatchCoverage:
                    return true;
                case ImmaterialResourceManager.Resource.EarthquakeCoverage:
                    return true;
                case ImmaterialResourceManager.Resource.DisasterCoverage:
                    return true;
                case ImmaterialResourceManager.Resource.TourCoverage:
                    return true;
                case ImmaterialResourceManager.Resource.PostService:
                    return true;
                case ImmaterialResourceManager.Resource.CashCollecting:
                    return true;
                case ImmaterialResourceManager.Resource.TaxBonus:
                    return true;
                case ImmaterialResourceManager.Resource.None:
                    return false;
                default:
                    return false;
            }
        }

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


        // This is an exact copy of CommonBuildingAI.GetHomeBehaviour to get info about wealth of the customers and number of visitors
        // This is a protected member and this data is not stored in Building object once used by the BuildingAI
        protected void GetHomeBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount, ref int homeCount, ref int aliveHomeCount, ref int emptyHomeCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Home) != 0)
                {
                    int aliveCount2 = 0;
                    int totalCount2 = 0;
                    instance.m_units.m_buffer[num].GetCitizenHomeBehaviour(ref behaviour, ref aliveCount2, ref totalCount2);
                    if (aliveCount2 != 0)
                    {
                        aliveHomeCount++;
                        aliveCount += aliveCount2;
                    }
                    if (totalCount2 != 0)
                    {
                        totalCount += totalCount2;
                    }
                    else
                    {
                        emptyHomeCount++;
                    }
                    homeCount++;
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
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
            if (ShowIt2Patcher.patched)
                CommonBuildingAI_Patches.GetHomeBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount, ref homeCount, ref aliveHomeCount, ref emptyHomeCount);
            else
                GetHomeBehaviour(buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount, ref homeCount, ref aliveHomeCount, ref emptyHomeCount);

            // Citizen.BehaviourData is not accessible, so we must use Building data
            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            int populace = behaviour.m_teenCount + behaviour.m_youngCount * 2 + behaviour.m_adultCount * 3 + behaviour.m_seniorCount * 3;
            if (populace != 0)
            {
                education = (education * 72 + (populace >> 1)) / populace;
            }

            // land value
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.LandValue, building.m_position, out var local);
            int waterValue = ShowLandValue(buildingId, ref building);

            // show it!
            _progTopName.text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            _progTopName.tooltip = "[Level 1]  ..14 | 15..29 | 30..44 | 45..59 | 60..  [Level 5]" + Environment.NewLine + // Infixo todo: change to slider
                $"Populace: Children {building.m_children} Teens {building.m_teens} Youngs {building.m_youngs} Adults {building.m_adults} Seniors {building.m_seniors}" + Environment.NewLine +
                $"Education: Low {building.m_education1} Medium {building.m_education2} High {building.m_education3}";
            _progTopProgress.text = progressTop.ToString();
            _progTopValue.text = education.ToString();
            _progBotName.text = Locale.Get(LocaleID.INFO_LANDVALUE_TITLE);
            _progBotName.tooltip = "[Level 1]   ..5 |  6..20 | 21..40 | 41..60 | 61..  [Level 5]"; // Infixo todo: change to slider
            _progBotProgress.text = progressBot.ToString();
            _progBotValue.text = local.ToString();
            _progBotValue.tooltip = $"Water value: {waterValue}";
        }

        private int CalculateAndShowSingleServiceValue(
            int resourceRate, int middleRate, int maxRate, int middleEffect, int maxEffect, // params for CalculateResourceEffect
            int divisor, int uiCtrl) // uiCtrl is a dict-key to access proper controls
        {
            int value = ImmaterialResourceManager.CalculateResourceEffect(resourceRate, middleRate, maxRate, middleEffect, maxEffect) / divisor;
            int maxValue = maxEffect / divisor;
            //_numbers[uiCtrl].text = value.ToString();
            //_maxVals[uiCtrl].text = maxValue.ToString();
            return value;
        }
        private int ProcessServiceValue(
            ushort[] resources, int index, // from CheckLocalResources
            ImmaterialResourceManager.Resource resourceType, // resource type
                                                             //int resourceRate, // not needed - taken from resources
            int middleRate, int maxRate, int middleEffect, int maxEffect, // params for CalculateResourceEffect
            int divisor, bool negative = false, int groundPollutionRate = 0)
        {
            int resourceRate = (resourceType == GroundPollution ? groundPollutionRate : resources[index + (int)resourceType]);
            int value = ImmaterialResourceManager.CalculateResourceEffect(resourceRate, middleRate, maxRate, middleEffect, maxEffect);
            //int maxValue = maxEffect / divisor;
            UIServiceBar uiBar = m_uiServices[resourceType];
            uiBar.Negative = negative;
            uiBar.BelowMid = (resourceRate < middleRate);
            if (divisor < 10)
            {
                // Service coverage
                uiBar.MaxValue = maxEffect / divisor;
                value /= divisor;
                uiBar.Value = value;
            }
            else
            {
                // Land value
                uiBar.MaxValue = (float)maxEffect / (float)divisor;
                uiBar.Value = (float)value / (float)divisor;
            }
            uiBar.Panel.tooltip = $"{resourceType}: rate={resourceRate} midMax={middleRate}->{maxRate} effect={middleEffect}->{maxEffect} div={divisor}";
            return value;
        }

        // This is an exact copy of IndustrialBuildingAI.CalculateServiceValue private method to get info about service coverage
        private int CalculateServiceValueIndustrial(ushort buildingID, ref Building data)
        {
            // UI adjustment
            m_uiServices[ImmaterialResourceManager.Resource.CargoTransport].Panel.Show();
            m_uiServices[ImmaterialResourceManager.Resource.EducationLibrary].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ChildCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ElderCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Health].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Wellbeing].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.FireHazard].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.CrimeRate].Panel.Hide();
            RescaleServiceBars(70f); // max is 50&100

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

            // original calculations
            int resourceRate = resources[index + 7]; // PublicTransport 7
            int resourceRate2 = resources[index + 2]; // PoliceDepartment 3
            int resourceRate3 = resources[index]; // HealthCare 0
            int resourceRate4 = resources[index + 6]; // DeathCare 1
            int resourceRate5 = resources[index + 25]; // PostService 8
            int resourceRate6 = resources[index + 1]; // FireDepartment 2
            int resourceRate7 = resources[index + 13]; // Entertainment 9
            int resourceRate8 = resources[index + 3]; // EducationElementary 4
            int resourceRate9 = resources[index + 4]; // EducationHighSchool 5
            int resourceRate10 = resources[index + 5]; // EducationUniversity 6
            int resourceRate11 = resources[index + 19]; // CargoTransport 11
            int resourceRate12 = resources[index + 8]; // NoisePollution 12
            int resourceRate13 = resources[index + 18]; // Abandonment --
            int resourceRate14 = resources[index + 20]; // RadioCoverage --
            int resourceRate15 = resources[index + 21]; // FirewatchCoverage 10
            int resourceRate16 = resources[index + 23]; // DisasterCoverage --
            int num = 0;
            num += CalculateAndShowSingleServiceValue(resourceRate, 100, 500, 50, 100, 3, 7); // PublicTransport
            num += CalculateAndShowSingleServiceValue(resourceRate2, 100, 500, 50, 100, 5, 3); // PoliceDepartment
            num += CalculateAndShowSingleServiceValue(resourceRate3, 100, 500, 50, 100, 5, 0); // HealthCare
            num += CalculateAndShowSingleServiceValue(resourceRate4, 100, 500, 50, 100, 5, 1); // DeathCare
            num += CalculateAndShowSingleServiceValue(resourceRate5, 100, 500, 50, 100, 5, 8); // PostService
            num += CalculateAndShowSingleServiceValue(resourceRate6, 100, 500, 50, 100, 2, 2); // FireDepartment
            num += CalculateAndShowSingleServiceValue(resourceRate7, 100, 500, 50, 100, 8, 9); // Entertainment
            num += CalculateAndShowSingleServiceValue(resourceRate8, 100, 500, 50, 100, 8, 4); // EducationElementary
            num += CalculateAndShowSingleServiceValue(resourceRate9, 100, 500, 50, 100, 8, 5); // EducationHighSchool
            num += CalculateAndShowSingleServiceValue(resourceRate10, 100, 500, 50, 100, 8, 6); // EducationUniversity
            num += CalculateAndShowSingleServiceValue(resourceRate11, 100, 500, 50, 100, 1, 11); // CargoTransport
            num += ImmaterialResourceManager.CalculateResourceEffect(resourceRate14, 50, 100, 80, 100) / 5; // RadioCoverage // I dont have this DLC
            num += CalculateAndShowSingleServiceValue(resourceRate15, 100, 1000, 0, 100, 5, 10); // FirewatchCoverage
            num += ImmaterialResourceManager.CalculateResourceEffect(resourceRate16, 50, 100, 80, 100) / 5; // DisasterCoverage // I dont have this DLC
            num -= CalculateAndShowSingleServiceValue(resourceRate12, 100, 500, 50, 100, 7, 12); // NoisePollution
            num -= ImmaterialResourceManager.CalculateResourceEffect(resourceRate13, 100, 500, 50, 100) / 7; // Abandonment // There is no UI overlay for that
            num -= ImmaterialResourceManager.CalculateResourceEffect(groundPollution, 50, 255, 50, 100) / 6; // Infixo todo: missing, there is no resource effect for that

            // debug check
            //Debug.Log($"ShowIt2.CalculateServiceValueIndustrial, buildingID={buildingID}, original={num}, new={value}, same? {value == num}");

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
            if (ShowIt2Patcher.patched)
                CommonBuildingAI_Patches.GetWorkBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            else
                GetWorkBehaviour(buildingId, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            if (aliveWorkerCount != 0)
            {
                education = (education * 20 + (aliveWorkerCount >> 1)) / aliveWorkerCount; // Infixo todo: it is 12 in Office
            }

            // service coverage
            int service = CalculateServiceValueIndustrial(buildingId, ref building);

            // show it!
            _progTopName.text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            _progTopName.tooltip = "[Level 1]  ..14 | 15..29 | 30..  [Level 3]" + Environment.NewLine + // Infixo todo: change to slider
                $"Workers: {aliveWorkerCount}, Education: Low {behaviour.m_educated1Count} Medium {behaviour.m_educated2Count} High {behaviour.m_educated3Count}";
            _progTopProgress.text = progressTop.ToString();
            _progTopValue.text = education.ToString();
            _progBotName.text = Locale.Get(LocaleID.POLICY_SERVICES);
            _progBotName.tooltip = "[Level 1]  ..29 | 30..59 | 60..  [Level 3]"; // Infixo todo: change to slider
            _progBotProgress.text = progressBot.ToString();
            _progBotValue.text = service.ToString();
            _progBotValue.tooltip = "";
        }

        // This is an exact copy of CommonBuildingAI.GetVisitBehaviour to get info about wealth of the customers and number of visitors
        // This is a protected member and this data is not stored in Building object once used by the BuildingAI
        private void GetVisitBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Visit) != 0)
                {
                    instance.m_units.m_buffer[num].GetCitizenVisitBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
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
            if (ShowIt2Patcher.patched)
                CommonBuildingAI_Patches.GetVisitBehaviour_Reverse(buildingAI, buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount); // aliveCount == visitorCount
            else
                GetVisitBehaviour(buildingId, ref building, ref behaviour, ref aliveCount, ref totalCount); // aliveCount == visitorCount
            int wealth = behaviour.m_wealth1Count + behaviour.m_wealth2Count * 2 + behaviour.m_wealth3Count * 3;
            if (aliveCount != 0)
            {
                wealth = (wealth * 18 + (aliveCount >> 1)) / aliveCount;
            }

            // land value
            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.LandValue, building.m_position, out var local);
            int waterValue = ShowLandValue(buildingId, ref building);

            // show it!
            _progTopName.text = "Wealth";
            _progTopName.tooltip = "[Level 1]  ..29 | 30..44 | 45..  [Level 3]" + Environment.NewLine + // Infixo todo: change to slider
                $"Visitors: {aliveCount}, Wealth: Low {behaviour.m_wealth1Count} Medium {behaviour.m_wealth2Count} High {behaviour.m_wealth3Count}";
            _progTopProgress.text = progressTop.ToString();
            _progTopValue.text = wealth.ToString();
            _progBotName.text = Locale.Get(LocaleID.INFO_LANDVALUE_TITLE);
            _progBotName.tooltip = "[Level 1]  ..20 | 21..40 | 41..  [Level 3]"; // Infixo todo: change to slider
            _progBotProgress.text = progressBot.ToString();
            _progBotValue.text = local.ToString();
            _progBotValue.tooltip = $"Water value: {waterValue}";
        }

        // This is an exact copy of CommonBuildingAI.GetWorkBehaviour to get info about education of the workers and number of workers
        // This is a protected member and this data is not stored in Building object once used by the BuildingAI
        protected void GetWorkBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    instance.m_units.m_buffer[num].GetCitizenWorkBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }


        // This is an exact copy of OfficeBuildingAI.CalculateServiceValue private method to get info about service coverage
        // Calls to CalculateResourceEffect are converted to ProcessServiceValue with exactly the same params
        private int CalculateServiceValueOffice(ushort buildingID, ref Building data)
        {
            // UI adjustment
            m_uiServices[ImmaterialResourceManager.Resource.CargoTransport].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.EducationLibrary].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ChildCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.ElderCare].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Health].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.Wellbeing].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.FireHazard].Panel.Hide();
            m_uiServices[ImmaterialResourceManager.Resource.CrimeRate].Panel.Hide();
            RescaleServiceBars(50f); // max is 33

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


            int resourceRate = resources[index + 7]; // PublicTransport
            int resourceRate2 = resources[index + 2]; // PoliceDepartment
            int resourceRate3 = resources[index]; // HealthCare
            int resourceRate4 = resources[index + 6]; // DeathCare
            int resourceRate5 = resources[index + 25]; // PostService
            int resourceRate6 = resources[index + 1]; // FireDepartment
            int resourceRate7 = resources[index + 13]; // Entertainment
            int resourceRate8 = resources[index + 3]; // EducationElementary
            int resourceRate9 = resources[index + 4]; // EducationHighSchool
            int resourceRate10 = resources[index + 5]; // EducationUniversity
            int resourceRate11 = resources[index + 8]; // NoisePollution
            int resourceRate12 = resources[index + 18]; // Abandonment
            int resourceRate13 = resources[index + 20]; // RadioCoverage
            int resourceRate14 = resources[index + 21]; // FirewatchCoverage
            int resourceRate15 = resources[index + 23]; // DisasterCoverage
            int num = 0;
            num += CalculateAndShowSingleServiceValue(resourceRate, 100, 500, 50, 100, 3, 7); // PublicTransport
            num += CalculateAndShowSingleServiceValue(resourceRate2, 100, 500, 50, 100, 5, 3); // PoliceDepartment
            num += CalculateAndShowSingleServiceValue(resourceRate3, 100, 500, 50, 100, 5, 0); // HealthCare
            num += CalculateAndShowSingleServiceValue(resourceRate4, 100, 500, 50, 100, 5, 1); // DeathCare
            num += CalculateAndShowSingleServiceValue(resourceRate5, 100, 500, 50, 100, 5, 8); // PostService
            num += CalculateAndShowSingleServiceValue(resourceRate6, 100, 500, 50, 100, 5, 2); // FireDepartment
            num += CalculateAndShowSingleServiceValue(resourceRate7, 100, 500, 50, 100, 6, 9); // Entertainment
            num += CalculateAndShowSingleServiceValue(resourceRate8, 100, 500, 50, 100, 7, 4); // EducationElementary
            num += CalculateAndShowSingleServiceValue(resourceRate9, 100, 500, 50, 100, 7, 5); // EducationHighSchool
            num += CalculateAndShowSingleServiceValue(resourceRate10, 100, 500, 50, 100, 7, 6); // EducationUniversity
            //num += ProcessServiceValue(resourceRate13, 50, 100, 80, 100, 5, 12); // RadioCoverage // I dont have this DLC
            num += ImmaterialResourceManager.CalculateResourceEffect(resourceRate13, 50, 100, 80, 100) / 5;
            num += CalculateAndShowSingleServiceValue(resourceRate14, 100, 1000, 0, 100, 5, 10); // FirewatchCoverage
            //num += ProcessServiceValue(resourceRate15, 50, 100, 80, 100, 5, 11); // DisasterCoverage // I dont have this DLC
            num += ImmaterialResourceManager.CalculateResourceEffect(resourceRate15, 50, 100, 80, 100) / 5;
            // negatives
            num -= CalculateAndShowSingleServiceValue(resourceRate11, 100, 500, 50, 100, 4, 11); // NoisePollution
            //num -= ProcessServiceValue(resourceRate12, 100, 500, 50, 100, 3, 14); // Abandonment // There is no UI overlay for that
            num -= ImmaterialResourceManager.CalculateResourceEffect(resourceRate12, 100, 500, 50, 100) / 3;
            //return num - ProcessServiceValue(groundPollution, 50, 255, 50, 100, 4; // Infixo todo: missing, there is no resource effect for that
            num -= ImmaterialResourceManager.CalculateResourceEffect(groundPollution, 50, 255, 50, 100) / 4;

            // debug check
            //Debug.Log($"ShowIt2.CalculateServiceValueOffice, buildingID={buildingID}, original={num}, new={value}, same? {value == num}");

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
            if (ShowIt2Patcher.patched)
                CommonBuildingAI_Patches.GetWorkBehaviour_Reverse(buildingAI, buildingID, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            else
                GetWorkBehaviour(buildingID, ref building, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount); // aliveWorkerCount is the one needed
            int education = behaviour.m_educated1Count + behaviour.m_educated2Count * 2 + behaviour.m_educated3Count * 3;
            if (aliveWorkerCount != 0)
            {
                education = (education * 12 + (aliveWorkerCount >> 1)) / aliveWorkerCount;
            }

            // service coverage
            int service = CalculateServiceValueOffice(buildingID, ref building);

            // show it!
            _progTopName.text = Locale.Get(LocaleID.INFO_EDUCATION_TITLE);
            _progTopName.tooltip = "[Level 1]  ..14 | 15..29 | 30..  [Level 3]" + Environment.NewLine + // Infixo todo: change to slider
                $"Workers: {aliveWorkerCount}, Education: Low {behaviour.m_educated1Count} Medium {behaviour.m_educated2Count} High {behaviour.m_educated3Count}";
            _progTopProgress.text = progressTop.ToString();
            _progTopValue.text = education.ToString();
            _progBotName.text = Locale.Get(LocaleID.POLICY_SERVICES);
            _progBotName.tooltip = "[Level 1]  ..44 | 45..89 | 90..  [Level 3]"; // Infixo todo: change to slider
            _progBotProgress.text = progressBot.ToString();
            _progBotValue.text = service.ToString();
            _progBotValue.tooltip = "";
        }

        private int ShowLandValue(ushort buildingID, ref Building data)
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

            value /= 10;

            // water proximity
            // int num31 = CalculateResourceEffect(num29, 33, 67, 300, 0) * Mathf.Max(0, 32 - num28) >> 5; // LV
            // if pollution > 32 then 0, if < 32 then $$ += water * (pollution/32)
            // if water < 33 then proportional to 300, if > 33 then reverse proportional i.e. 0 -> 300 -> 0
            // ideal water adds +30 $$
            int waterValue = realValue - value;
            //Debug.Log($"ShowIt2.ShowLandValue: value={value} real={realValue} water={waterValue}");
            return waterValue;
        }
    }
}