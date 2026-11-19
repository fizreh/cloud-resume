namespace VisitorCounterApi.Configuration
{
    // The setting name is written once, so a typo can't happen in two places (DRY).
    public static class SettingNames
    {
        public const string SqlConnectionString = "SqlConnectionString";
        public const string LanguageEndpoint = "LanguageEndpoint";
        public const string LanguageKey = "LanguageKey";
    }
}