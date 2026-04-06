using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KronyxEditor.Docking
{
    public static class DockContentRegistry
    {
        private static readonly Dictionary<string, Func<FrameworkElement>> _factories =
            new Dictionary<string, Func<FrameworkElement>>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string contentId, Func<FrameworkElement> factory)
        {
            _factories[contentId] = factory;
        }

        public static FrameworkElement Create(string contentId)
        {
            if (!_factories.TryGetValue(contentId, out var factory))
            {
                return new TextBlock { Text = $"Unknown content: {contentId}", Margin = new Thickness(10) };
            }

            return factory();
        }
    }
}
