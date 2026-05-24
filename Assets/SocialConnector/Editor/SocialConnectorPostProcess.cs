#if UNITY_EDITOR_OSX
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SocialConnector
{
    public class SocialConnectorPostProcess
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            AddLanguage(path, "ja");
            AddPermissions(path, new[]
            {
                new KeyValuePair<string, string>("NSPhotoLibraryUsageDescription", "Save the Application's screenshot.")
            });
        }

        private static void AddLanguage(string path, params string[] languages)
        {
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            const string localizationKey = "CFBundleLocalizations";
            PlistElementArray localizations = plist.root.values
                .Where(kv => kv.Key == localizationKey)
                .Select(kv => kv.Value)
                .Cast<PlistElementArray>()
                .FirstOrDefault();

            if (localizations == null)
            {
                localizations = plist.root.CreateArray(localizationKey);
            }

            foreach (string language in languages)
            {
                if (!localizations.values.Select(el => el.AsString()).Contains(language))
                {
                    localizations.AddString(language);
                }
            }

            plist.WriteToFile(plistPath);
        }

        private static void AddPermissions(string path, params KeyValuePair<string, string>[] permissions)
        {
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            foreach (KeyValuePair<string, string> permission in permissions)
            {
                int count = plist.root.values
                    .Where(kv => kv.Key == permission.Key)
                    .Select(kv => kv.Value)
                    .Count();

                if (count == 0)
                {
                    plist.root.SetString(permission.Key, permission.Value);
                }
            }

            plist.WriteToFile(plistPath);
        }
    }
}
#endif
