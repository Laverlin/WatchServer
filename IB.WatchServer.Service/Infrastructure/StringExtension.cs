using System;
using System.Globalization;
using System.Text;
using LinqToDB.Common;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helpers for String 
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Removes any non spacing marks from unicode string. This will strip out diacritic marks but leave basic char in place.
        /// e.g. crème brûlée will become creme brulee
        /// </summary>
        /// <param name="originalString">unicode string with diacritics</param>
        /// <returns>Stripped string without diacritic symbols, and NULL if null has been passed</returns>
        /// <remarks>
        /// This method may not work properly in some linux docker environment, e. g. Alpine, as .NET Core gets the normalization behavior
        /// from the underlying OS, Alpine does not support some unicode features by default, so .Net is running in
        /// DOTNET_SYSTEM_GLOBALIZATION_INVARIANT enabled mode. As a result, calling this method has no effects, you'll get unchanged string back.
        /// more info here: https://github.com/dotnet/coreclr/issues/23831
        /// </remarks>
        public static string StripDiacritics(this String originalString)
        {
            if (originalString.IsNullOrEmpty())
                return originalString;

            var normalizedString = originalString.Normalize(NormalizationForm.FormD);
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
