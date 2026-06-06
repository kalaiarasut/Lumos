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
            
            // Remove existing theme color dictionaries only (not ModernControls)
            var existingThemes = dictionaries
                .Where(d => d.Source != null && 
                       (d.Source.OriginalString.EndsWith("Light.xaml") || 
                        d.Source.OriginalString.EndsWith("Dark.xaml")))
                .ToList();
                
            foreach (var existingTheme in existingThemes)
            {
                dictionaries.Remove(existingTheme);
            }

            // Add the new theme dictionary (insert before ModernControls so it can reference theme brushes)
            var modernControlsIndex = dictionaries
                .Select((d, i) => new { d, i })
                .FirstOrDefault(x => x.d.Source != null && x.d.Source.OriginalString.Contains("ModernControls"))?.i ?? -1;

            var newTheme = new ResourceDictionary { Source = new Uri(themeUri) };
            if (modernControlsIndex >= 0)
                dictionaries.Insert(modernControlsIndex, newTheme);
            else
                dictionaries.Add(newTheme);
        }
    }
}
