using System;
using System.Linq;
using System.Windows;

namespace Lumos.UI.WPF
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeMode)
        {
            var application = System.Windows.Application.Current;
            if (application == null)
                return;

            string themeUri = themeMode.ToLowerInvariant() == "dark" 
                ? "pack://application:,,,/Lumos;component/UI/WPF/Themes/Dark.xaml"
                : "pack://application:,,,/Lumos;component/UI/WPF/Themes/Light.xaml";

            var dictionaries = application.Resources.MergedDictionaries;
            
            // Remove existing theme dictionaries
            var existingThemes = dictionaries
                .Where(d => d.Source != null && d.Source.OriginalString.Contains("/UI/WPF/Themes/"))
                .ToList();
                
            foreach (var existingTheme in existingThemes)
            {
                dictionaries.Remove(existingTheme);
            }

            // Add the new theme dictionary
            dictionaries.Add(new ResourceDictionary { Source = new Uri(themeUri) });
        }
    }
}
