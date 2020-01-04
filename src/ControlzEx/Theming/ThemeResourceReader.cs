﻿namespace ControlzEx.Theming
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Internal;

    public abstract class ThemeResourceReader : DependencyObject
    {
        private readonly Assembly assembly;
        private readonly string assemblyName;
        private readonly string[] resourceNames;

        protected ThemeResourceReader(bool registerAtThemeManager)
        {
            this.assembly = this.GetType().Assembly;
            this.assemblyName = this.assembly.GetName().Name;

            this.resourceNames = this.assembly.GetManifestResourceNames();

            if (registerAtThemeManager)
            {
                ThemeManager.RegisterThemeResourceReader(this);
            }
        }

        public string GeneratorParametersResourceName { get; protected set; } = "GeneratorParameters.json";

        public string ThemeTemplateResourceName { get; protected set; } = "Theme.Template.xaml";

        protected virtual bool ResourceNamesMatch(string resourceName, string value)
        {
            if (resourceName.Equals(value, StringComparison.OrdinalIgnoreCase)
                || (resourceName.StartsWith(this.assemblyName, StringComparison.OrdinalIgnoreCase) && resourceName.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        public abstract void FillColorSchemeValues(Dictionary<string, string> values, Color accentColor, Color accentColor80Percent, Color accentColor60Percent, Color accentColor40Percent, Color accentColor20Percent, Color highlightColor, Color idealForegroundColor);

        public virtual string GetThemeGeneratorParametersContent()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (this.ResourceNamesMatch(resourceName, this.GeneratorParametersResourceName) == false)
                {
                    continue;
                }

                using (var stream = this.assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream.IsNull())
                    {
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public virtual string GetThemeTemplateContent()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (this.ResourceNamesMatch(resourceName, this.ThemeTemplateResourceName) == false)
                {
                    continue;
                }

                using (var stream = this.assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream.IsNull())
                    {
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public virtual bool IsPotentialThemeResourceDictionary(DictionaryEntry dictionaryEntry)
        {
            var stringKey = dictionaryEntry.Key as string;
            if (stringKey.IsNull()
                || stringKey.IndexOf("/themes/", StringComparison.OrdinalIgnoreCase) == -1
                || stringKey.EndsWith(".baml", StringComparison.OrdinalIgnoreCase) == false
                || stringKey.EndsWith("generic.baml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public virtual LibraryTheme GetLibraryTheme(DictionaryEntry dictionaryEntry)
        {
            if (this.IsPotentialThemeResourceDictionary(dictionaryEntry) == false)
            {
                return null;
            }

            var stringKey = dictionaryEntry.Key as string;

            if (string.IsNullOrEmpty(stringKey))
            {
                return null;
            }

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{this.assemblyName};component/{stringKey.Replace(".baml", ".xaml")}")
            };

            if (resourceDictionary.MergedDictionaries.Count == 0
                && ThemeManager.IsThemeDictionary(resourceDictionary))
            {
                return new LibraryTheme(resourceDictionary, false);
            }

            return null;
        }

        public virtual IEnumerable<LibraryTheme> GetLibraryThemes()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (resourceName.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                var info = this.assembly.GetManifestResourceInfo(resourceName);
                if (info.IsNull()
                    || info.ResourceLocation == ResourceLocation.ContainedInAnotherAssembly)
                {
                    continue;
                }

                var resourceStream = this.assembly.GetManifestResourceStream(resourceName);

                if (resourceStream.IsNull())
                {
                    continue;
                }

                using (var reader = new ResourceReader(resourceStream))
                {
                    foreach (DictionaryEntry dictionaryEntry in reader)
                    {
                        var theme = this.GetLibraryTheme(dictionaryEntry);

                        if (theme.IsNotNull())
                        {
                            yield return theme;
                        }
                    }
                }
            }
        }
    }
}