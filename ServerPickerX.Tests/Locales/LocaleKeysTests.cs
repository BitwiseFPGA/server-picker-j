using System.Xml.Linq;

namespace ServerPickerX.Tests.Locales
{
    // A key missing from one language shows up as empty text in that language only,
    // and nothing else catches it at compile time
    public class LocaleKeysTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        [Fact]
        public void EveryLanguage_HasTheSameKeysAsEnglish()
        {
            string localeFolder = TestPaths.FromRepositoryRoot("ServerPickerX", "Locales");

            HashSet<string> englishKeys = ReadKeys(Path.Combine(localeFolder, "Locale_en-us.axaml"));

            string[] localeFiles = Directory.GetFiles(localeFolder, "Locale_*.axaml");

            Assert.Equal(9, localeFiles.Length);

            foreach (string localeFile in localeFiles)
            {
                HashSet<string> keys = ReadKeys(localeFile);

                Assert.True(
                    keys.SetEquals(englishKeys),
                    $"{Path.GetFileName(localeFile)} is missing [{string.Join(", ", englishKeys.Except(keys))}] " +
                    $"and has extra [{string.Join(", ", keys.Except(englishKeys))}]"
                    );
            }
        }

        private static HashSet<string> ReadKeys(string path)
        {
            return XDocument.Load(path).Root!
                .Elements()
                .Select(element => (string?)element.Attribute(Xaml + "Key"))
                .Where(key => !string.IsNullOrEmpty(key))
                .Select(key => key!)
                .ToHashSet(StringComparer.Ordinal);
        }
    }
}
