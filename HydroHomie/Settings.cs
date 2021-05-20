namespace HydroHomie
{
    public class Settings
    {
        public bool EnableAlerts { get; set; }
        public bool MuteAlerts { get; set; }
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public bool UseCustomTexts { get; set; }
        public bool UseCustomSounds { get; set; }
        public int AlertFrequency { get; set; }
        public int AlertDuration { get; set; }
    }
}
