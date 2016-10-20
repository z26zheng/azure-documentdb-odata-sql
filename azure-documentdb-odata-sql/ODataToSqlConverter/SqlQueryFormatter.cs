namespace Microsoft.Azure.Documents.OData.Sql
{
    public class SQLQueryFormatter : QueryFormatterBase
    {
        /// <summary>
        /// fieldName => c.fieldName
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override string TranslateFieldName(string fieldName)
        {
            return string.Concat(Constants.SQLFieldNameSymbol, fieldName.Trim());
        }

        /// <summary>
        /// Convert value to SQL format: Namespace'enumVal' => c.enumVal
        /// </summary>
        /// <param name="value">the enum value</param>
        /// <param name="nameSpace">Namespace of the enum type</param>
        /// <returns>enumValue without the namespace</returns>
        public override string TranslateEnumValue(string value, string nameSpace)
        {
            return string.Concat(Constants.SQLFieldNameSymbol, value.Substring(nameSpace.Length).Trim());
        }

        /// <summary>
        /// Convert fieldname (parent and child) to SQL format: "class/field" => "c.class.field'"
        /// </summary>
        /// <param name="source">the parent field</param>
        /// <param name="edmProperty">the child field</param>
        /// <returns>The translated source</returns>
        public override string TranslateSource(string source, string edmProperty)
        {
            var str = string.Concat(source.Trim(), Constants.SymbolDot, edmProperty.Trim());
            return str.StartsWith(Constants.SQLFieldNameSymbol) ? str : string.Concat(Constants.SQLFieldNameSymbol, str);
        }

    }
}
