using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.OData.Sql
{
    public abstract class QueryFormatterBase
    {
        /// <summary>
        /// method to translate fieldName
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public abstract string TranslateFieldName(string fieldName);

        /// <summary>
        /// method to translate enum values
        /// </summary>
        /// <param name="value">the enum value</param>
        /// <param name="nameSpace">Namespace of the enum type</param>
        /// <returns>enumValue without the namespace</returns>
        public abstract string TranslateEnumValue(string enumValue, string nameSpace);

        /// <summary>
        /// method to convert parent/child field
        /// </summary>
        /// <param name="source">the parent field</param>
        /// <param name="edmProperty">the child field</param>
        /// <returns>The translated source</returns>
        public abstract string TranslateSource(string source, string edmProperty);
    }
}
