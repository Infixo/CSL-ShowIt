namespace ShowIt2
{
    [ConfigurationPath("ShowItConfig.xml")]
    public class ModConfig
    {
        public bool ConfigUpdated { get; set; }
        public bool ShowPanel { get; set; } = true;
        public string Alignment { get; set; } = "Right";
        public float Scaling { get; set; } = 0.8f;
        //public float IndicatorsPanelChartHorizontalSpacing { get; set; } = 15f;
        public float Spacing { get; set; } = 2f;
        //public float IndicatorsPanelNumberTextScale { get; set; } = 0.7f;
        //public string IndicatorsPanelLegend { get; set; } = "Icons";
        //public float IndicatorsPanelIconSize { get; set; } = 25f;
        //public float IndicatorsPanelLabelTextScale { get; set; } = 0.6f;

        private static ModConfig instance;

        public static ModConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Configuration<ModConfig>.Load();
                }

                return instance;
            }
        }

        public void Save()
        {
            Configuration<ModConfig>.Save();
            ConfigUpdated = true;
        }
    }
}