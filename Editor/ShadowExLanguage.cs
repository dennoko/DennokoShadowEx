#if UNITY_EDITOR
using lilToon;

namespace dennokoworks
{
    internal static class ShadowExLanguage
    {
        public static bool IsJapanese
        {
            get
            {
                string lang = lilLanguageManager.langSet.languageName;
                return !string.IsNullOrEmpty(lang) && lang.StartsWith("ja");
            }
        }

        public static string Get(string ja, string en)
        {
            return IsJapanese ? ja : en;
        }
    }
}
#endif
