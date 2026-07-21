using System;
using System.Linq;
using System.Xml.Linq;

namespace AdieLab.TeacherTraining
{
    public static class QuestAndroidManifestPolicy
    {
        private const string EyeTrackingFeature = "oculus.software.eye_tracking";
        private const string MicrophoneFeature = "android.hardware.microphone";
        private const string RecordAudioPermission = "android.permission.RECORD_AUDIO";
        private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public static string Apply(string manifestXml)
        {
            if (string.IsNullOrWhiteSpace(manifestXml)) return manifestXml ?? string.Empty;
            XDocument document = XDocument.Parse(manifestXml, LoadOptions.PreserveWhitespace);
            XElement root = document.Root;
            if (root == null) return manifestXml;
            bool changed = MakeFeatureOptional(document, EyeTrackingFeature);
            if (!root.Elements("uses-permission").Any(element => string.Equals((string)element.Attribute(AndroidNamespace + "name"), RecordAudioPermission, StringComparison.Ordinal)))
            {
                root.AddFirst(new XElement("uses-permission", new XAttribute(AndroidNamespace + "name", RecordAudioPermission)));
                changed = true;
            }
            XElement microphone = root.Elements("uses-feature").FirstOrDefault(element => string.Equals((string)element.Attribute(AndroidNamespace + "name"), MicrophoneFeature, StringComparison.Ordinal));
            if (microphone == null)
            {
                root.Add(new XElement("uses-feature", new XAttribute(AndroidNamespace + "name", MicrophoneFeature), new XAttribute(AndroidNamespace + "required", "false")));
                changed = true;
            }
            else if (!string.Equals((string)microphone.Attribute(AndroidNamespace + "required"), "false", StringComparison.OrdinalIgnoreCase))
            {
                microphone.SetAttributeValue(AndroidNamespace + "required", "false");
                changed = true;
            }
            return changed ? document.ToString(SaveOptions.DisableFormatting) : manifestXml;
        }

        public static string MakeEyeTrackingOptional(string manifestXml)
        {
            if (string.IsNullOrWhiteSpace(manifestXml)) return manifestXml ?? string.Empty;
            XDocument document = XDocument.Parse(manifestXml, LoadOptions.PreserveWhitespace);
            return MakeFeatureOptional(document, EyeTrackingFeature) ? document.ToString(SaveOptions.DisableFormatting) : manifestXml;
        }

        private static bool MakeFeatureOptional(XDocument document, string featureName)
        {
            bool changed = false;
            foreach (XElement feature in document.Descendants("uses-feature").Where(element => string.Equals((string)element.Attribute(AndroidNamespace + "name"), featureName, StringComparison.Ordinal)))
            {
                XAttribute required = feature.Attribute(AndroidNamespace + "required");
                if (required == null || !string.Equals(required.Value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    feature.SetAttributeValue(AndroidNamespace + "required", "false");
                    changed = true;
                }
            }
            return changed;
        }
    }
}