using System;

namespace Microsoft.Azure.Documents.OData.Sql
{
    /// <summary>
    /// string formmater for OData to Sql converter
    /// </summary>
    public class SQLQueryFormatter : IQueryFormatter
    {
        /// <summary>
        /// constructor
        /// </summary>
        public SQLQueryFormatter
            ()
        {
            startLetter = 'a';
            startLetter--;
        }
        /// <summary>
        /// fieldName => c.fieldName
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string TranslateFieldName(string fieldName)
        {
            return string.Concat(Constants.SQLFieldNameSymbol, Constants.SymbolDot, fieldName.Trim());
        }

        /// <summary>
        /// Convert value to SQL format: Namespace'enumVal' => c.enumVal
        /// </summary>
        /// <param name="value">the enum value</param>
        /// <param name="nameSpace">Namespace of the enum type</param>
        /// <returns>enumValue without the namespace</returns>
        public string TranslateEnumValue(string value, string nameSpace)
        {
            return string.Concat(value.Substring(nameSpace.Length).Trim());
        }

        /// <summary>
        /// Convert fieldname (parent and child) to SQL format: "class/field" => "c.class.field'"
        /// </summary>
        /// <param name="source">the parent field</param>
        /// <param name="edmProperty">the child field</param>
        /// <returns>The translated source</returns>
        public string TranslateSource(string source, string edmProperty)
        {
            var str = string.Concat(source.Trim(), Constants.SymbolDot, edmProperty.Trim());
            return str.StartsWith(Constants.SQLFieldNameSymbol + Constants.SymbolDot) ? str : string.Concat(Constants.SQLFieldNameSymbol, Constants.SymbolDot, str);
        }

        /// <summary>
        /// Convert functionName to SQL format: funtionName => FUNCTIONNAME
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        public string TranslateFunctionName(string functionName)
        {
            switch (functionName)
            {
             
                case Constants.KeywordToUpper:
                    return Constants.SQLUpperSymbol;

                case Constants.KeywordToLower:
                    return Constants.SQLLowerSymbol;

                case Constants.KeywordIndexOf:
                    return Constants.SQLIndexOfSymbol;

                case Constants.KeywordTrim:
                    return $"{Constants.SQLLtrimSymbol}{Constants.SymbolOpenParen}{Constants.SQLRtrimSymbol}";

                default:
                    return functionName.ToUpper();
            }
        }
        private char startLetter;

        /// <summary>
        /// returns e.g. JOIN a IN c.companies
        /// </summary>
        /// <param name="joinCollection"></param>
        public string TranslateJoinClause(string joinCollection)
        {
            startLetter++;
            //startLetter becomes 'b', 'c' etc
            return string.Concat(Constants.SQLJoinSymbol, 
                Constants.SymbolSpace, startLetter, 
                Constants.SymbolSpace, 
                Constants.SQLInKeyword, 
                Constants.SymbolSpace, 
                Constants.SQLFieldNameSymbol, 
                Constants.SymbolDot, joinCollection);

        }
        /// <summary>
        /// translate any expression to a where clause
        /// </summary>
        /// <param name="source"></param>
        /// <param name="edmProperty"></param>
        public string TranslateJoinClause(string source, string edmProperty)
        {
            return string.Concat(startLetter, Constants.SymbolDot, edmProperty);
        }
    }
}
