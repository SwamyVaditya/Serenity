
namespace Serenity.Localization
{
    using Extensibility;
    using Newtonsoft.Json.Linq;
    using Serenity.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public static class NestedLocalTextRegistration
    {
        public static void Initialize()
        {
            var assemblies = ExtensibilityHelper.SelfAssemblies;

            foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<NestedLocalTextsAttribute>();
                    if (attr != null)
                    {
                        Initialize(type);
                    }
                }
        }

        private static void Initialize(Type type, string languageID = LocalText.InvariantLanguageID)
        {
            Initialize(type, languageID, "");
        }

        private static void Initialize(Type type, string languageID, string prefix)
        {
            var provider = Dependency.Resolve<ILocalTextRegistry>();
            foreach (var member in type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var fi = member as FieldInfo;
                if (fi != null &&
                    fi.FieldType == typeof(LocalText))
                {
                    var value = fi.GetValue(null) as LocalText;
                    if (value != null)
                    {
                        provider.Add(languageID, prefix + fi.Name, value.Key);
                        fi.SetValue(null, new LocalText(prefix + fi.Name));
                    }
                }
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var name = nested.Name;
                if (name.EndsWith("_"))
                    name = name.Substring(0, name.Length - 1);

                Initialize(nested, languageID, prefix + name + ".");
            }
        }

        public static void AddFromDictionary(IDictionary<string, JToken> obj, string prefix, string languageID)
        {
            if (obj == null)
                return;

            prefix = prefix ?? "";
            var registry = Dependency.Resolve<ILocalTextRegistry>();

            foreach (var k in obj)
            {
                var actual = prefix + k.Key;
                var o = k.Value;
                if (o is IDictionary<string, JToken>)
                    AddFromDictionary((IDictionary<string, JToken>)o, actual + ".", languageID);
                else
                {
                    registry.Add(languageID, actual, o.ToString());
                }
            }
        }

        public static void AddFromScripts(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                var texts = JsonConfigHelper.LoadConfig<Dictionary<string, JToken>>(file);
                var langID = Path.GetFileNameWithoutExtension(Path.GetFileName(file));

                var idx = langID.LastIndexOf(".");
                if (idx >= 0)
                    langID = langID.Substring(idx + 1);

                NestedLocalTextRegistration.AddFromDictionary(texts, "", langID);
            }
        }
    }
}