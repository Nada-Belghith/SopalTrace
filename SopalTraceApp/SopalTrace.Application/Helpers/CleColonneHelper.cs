using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SopalTrace.Application.Helpers
{
    public static class CleColonneHelper
    {
        public static string Normalize(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            // 1. Trim and Upper
            var normalized = label.Trim().ToUpperInvariant();

            // 2. Remove diacritics (accents)
            normalized = RemoveDiacritics(normalized);

            // 3. Replace spaces and invalid characters with underscores
            normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", "_");

            // 4. Remove leading/trailing underscores
            normalized = normalized.Trim('_');

            return normalized;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
