using System.Collections.Generic;
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

		public static (string[] JoinClauses, string JoinCondition) FindAndTranslateJoin(this string translation)
		{
			const string anyRegEx = @"(join (.) IN ([^\/]*))\/any\((.):c\..\.([^\s]*)([^']*)('[^']*')\)";
			const string clauseTemplate = "{0}.{1} {2} {3}";
			var finalTranslation = translation;

			var matches = Regex.Matches(translation, anyRegEx, RegexOptions.IgnoreCase);
			var joinClauses = new List<string>();

			foreach (Match match in matches)
			{
				var collection = match.Groups[3];
				var letter = match.Groups[4];
				var property = match.Groups[5];
				var operand = match.Groups[6].Value.Trim();
				var value = match.Groups[7];

				var finalValue = string.Format(clauseTemplate,letter, property, operand, value);
				var join = $"{Constants.SQLJoinSymbol} {letter} {Constants.SQLInSumbol} {collection} ";
				joinClauses.Add(join);
				finalTranslation = finalTranslation.Replace(match.Value, finalValue);
			}

			return (joinClauses.ToArray(), finalTranslation);
		}
	}
}
