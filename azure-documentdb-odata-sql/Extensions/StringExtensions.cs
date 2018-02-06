using System.Text.RegularExpressions;

namespace Microsoft.Azure.Documents.OData.Sql.Extensions
{
	public static class StringExtensions
	{
		public static string FindAndTranslateAny(this string translation)
		{
			const string anyRegEx = @"(c\.[^\/]*)\/any\([^:]*:[^=]*= ([^)]*)\)";
			const string arrayContainsFormat = "ARRAY_CONTAINS({0},{1})";
			var finalTranslation = translation;

			var matches = Regex.Matches(translation, anyRegEx);
			foreach (Match match in matches)
			{
				var array = match.Groups[1];
				var value = match.Groups[2];
				var finalValue = string.Format(arrayContainsFormat, array, value);
				finalTranslation = finalTranslation.Replace(match.Value, finalValue);
			}

			return finalTranslation;
		}
	}
}
