using System.Globalization;
using System.Linq;
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
        /// <param name="diacriticString">unicode string with diacritics</param>
        /// <returns>Stripped string without diacritic symbols, and NULL if null has been passed</returns>
        /// <remarks>
        /// This method may not work properly in some linux docker environment, e. g. Alpine, as .NET Core gets the normalization behavior
        /// from the underlying OS, Alpine does not support some unicode features by default, so .Net is running in
        /// DOTNET_SYSTEM_GLOBALIZATION_INVARIANT enabled mode. As a result, calling this method has no effects, you'll get unchanged string back.
        /// more info here: https://github.com/dotnet/coreclr/issues/23831
        /// </remarks>
        public static string StripDiacritics(this string diacriticString)
        {
            if (diacriticString.IsNullOrEmpty())
                return diacriticString;

            var normalizedString = diacriticString.Normalize(NormalizationForm.FormD);

            var preprocessed = from c in normalizedString
                let unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)
                where unicodeCategory != UnicodeCategory.NonSpacingMark
                select c;

            return new string(preprocessed.ToArray()).Normalize(NormalizationForm.FormC).Replace('’', '\'');
        }

    }
}
