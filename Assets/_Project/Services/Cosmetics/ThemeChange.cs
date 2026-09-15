using Line98.Data;

namespace Line98.Services
{
    public readonly struct ThemeChange
    {
        public readonly ThemeCategory Category;
        public readonly string ThemeId;

        public ThemeChange(ThemeCategory category, string themeId)
        {
            Category = category;
            ThemeId = themeId;
        }
    }
}
