using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData;

namespace Microsoft.Azure.Documents.OData.Sql
{
     /// <summary>
    /// Build QueryNode to string Representation 
    /// </summary>
    internal class ODataNodeToStringBuilder : QueryNodeVisitor<string>
    {
        /// <summary>
        /// whether translating search options or others
        /// </summary>
        private bool searchFlag;

        /// <summary>s
        /// Gets the formatter to format the query
        /// </summary>
        private QueryFormatterBase QueryFormatter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataNodeToStringBuilder"/> class
        /// </summary>
        /// <param name="queryFormatter">the query format class</param>
        public ODataNodeToStringBuilder(QueryFormatterBase queryFormatter)
        {
            this.QueryFormatter = queryFormatter;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ODataNodeToStringBuilder"/> class from being created
        /// </summary>
        private ODataNodeToStringBuilder()
        {
        }

        /// <summary>
        /// Translates a <see cref="AllNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(Microsoft.OData.UriParser.AllNode node)
        {
            string result = string.Concat(this.TranslateNode(node.Source), Constants.SymbolForwardSlash, Constants.KeywordAll, Constants.SymbolOpenParen, node.CurrentRangeVariable.Name, Constants.SymbolColon, this.TranslateNode(node.Body), Constants.SymbolClosedParen);
            return result;
        }

        /// <summary>
        /// Translates a <see cref="AnyNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(AnyNode node)
        {
            if (node.CurrentRangeVariable == null && node.Body.Kind == QueryNodeKind.Constant)
            {
                return string.Concat(this.TranslateNode(node.Source), Constants.SymbolForwardSlash, Constants.KeywordAny, Constants.SymbolOpenParen, Constants.SymbolClosedParen);
            }
            else
            {
                return string.Concat(this.TranslateNode(node.Source), Constants.SymbolForwardSlash, Constants.KeywordAny, Constants.SymbolOpenParen, node.CurrentRangeVariable.Name, ":", this.TranslateNode(node.Body), Constants.SymbolClosedParen);
            }
        }

        /// <summary>
        /// Translates a <see cref="BinaryOperatorNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(BinaryOperatorNode node)
        {
            var leftNode = node.Left;
            while (leftNode != null && leftNode.Kind == QueryNodeKind.Convert)
            {
                leftNode = (leftNode as ConvertNode).Source;
            }

            var rightNode = node.Right;
            while (rightNode != null && rightNode.Kind == QueryNodeKind.Convert)
            {
                rightNode = (rightNode as ConvertNode).Source;
            }

            var left = this.TranslateNode(node.Left);
            if (leftNode.Kind == QueryNodeKind.BinaryOperator && this.TranslateBinaryOperatorPriority(((BinaryOperatorNode)leftNode).OperatorKind) < this.TranslateBinaryOperatorPriority(node.OperatorKind))
            {
                left = string.Concat(Constants.SymbolOpenParen, left, Constants.SymbolClosedParen);
            }

            var right = this.TranslateNode(node.Right);
            if (rightNode.Kind == QueryNodeKind.BinaryOperator && this.TranslateBinaryOperatorPriority(((BinaryOperatorNode)rightNode).OperatorKind) < this.TranslateBinaryOperatorPriority(node.OperatorKind))
            {
                right = string.Concat(Constants.SymbolOpenParen, right, Constants.SymbolClosedParen);
            }

            return string.Concat(left, Constants.SymbolSpace, this.BinaryOperatorNodeToString(node.OperatorKind), Constants.SymbolSpace, right);
        }

        /// <summary>
        /// Translates a <see cref="CollectionNavigationNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(CollectionNavigationNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.NavigationProperty.Name, node.NavigationSource);
        }

        /// <summary>
        /// Translates a <see cref="CollectionPropertyAccessNode"/> into a corresponding.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(CollectionPropertyAccessNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.Property.Name);
        }

        /// <summary>
        /// Translates a <see cref="CollectionPropertyAccessNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(ConstantNode node)
        {
            if (node.Value == null)
            {
                return Constants.KeywordNull;
            }

            if (node.TypeReference.Definition.TypeKind == EdmTypeKind.Enum)
            {
                return this.QueryFormatter.TranslateEnumValue(node.LiteralText, (node.Value as ODataEnumValue).TypeName);
            }

            return node.LiteralText;
        }

        /// <summary>
        /// Translates a <see cref="ConvertNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(ConvertNode node)
        {
            return this.TranslateNode(node.Source);
        }

        /// <summary>
        /// Translates a <see cref="CollectionResourceCastNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string of EntityCollectionCastNode.</returns>
        public override string Visit(CollectionResourceCastNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.ItemStructuredType.Definition.ToString());
        }

        ///// <summary>
        ///// Visit an CollectionResourceCastNode
        ///// </summary>
        ///// <param name="node">the node to visit</param>
        ///// <returns>The translated string of CollectionPropertyCastNode</returns>
        //public override string Visit(CollectionPropertyCastNode node)
        //{
        //    return this.TranslatePropertyAccess(node.Source, node.CollectionType.Definition.ToString());
        //}

        /// <summary>
        /// Translates a <see cref="ResourceRangeVariableReferenceNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(ResourceRangeVariableReferenceNode  node)
        {
            if (node.Name == "$it")
            {
                return string.Empty;
            }
            else
            {
                return node.Name;
            }
        }

        /// <summary>
        /// Translates a <see cref="NonResourceRangeVariableReferenceNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(NonResourceRangeVariableReferenceNode node)
        {
            return node.Name;
        }

        /// <summary>
        /// Translates a <see cref="SingleResourceCastNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleResourceCastNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.StructuredTypeReference.Definition.ToString());
        }

        ///// <summary>
        ///// Translates a <see cref="SingleValueCastNode"/> into a corresponding <see cref="string"/>.
        ///// </summary>
        ///// <param name="node">The node to translate.</param>
        ///// <returns>The translated string of SingleValueCastNode.</returns>
        //public override string Visit(Microsoft.OData.UriParser.SingleResourceCastNode node)
        //{
        //    return this.TranslatePropertyAccess(node.Source, node.TypeReference.Definition.ToString());
        //}

        /// <summary>
        /// Translates a <see cref="SingleNavigationNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleNavigationNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.NavigationProperty.Name, node.NavigationSource);
        }

        /// <summary>
        /// Translates a <see cref="SingleResourceFunctionCallNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleResourceFunctionCallNode node)
        {
            string result = node.Name;
            if (node.Source != null)
            {
                result = this.TranslatePropertyAccess(node.Source, result);
            }

            return this.TranslateFunctionCall(result, node.Parameters);
        }

        /// <summary>
        /// Translates a <see cref="SingleValueFunctionCallNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleValueFunctionCallNode node)
        {
            string result = node.Name;
            if (node.Source != null)
            {
                result = this.TranslatePropertyAccess(node.Source, result);
            }

            return this.TranslateFunctionCall(result, node.Parameters);
        }

        /// <summary>
        /// Translates a <see cref="CollectionFunctionCallNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string of CollectionFunctionCallNode.</returns>
        public override string Visit(CollectionFunctionCallNode node)
        {
            string result = node.Name;
            if (node.Source != null)
            {
                result = this.TranslatePropertyAccess(node.Source, result);
            }

            return this.TranslateFunctionCall(result, node.Parameters);
        }

        /// <summary>
        /// Translates a <see cref="CollectionResourceFunctionCallNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string of EntityCollectionFunctionCallNode.</returns>
        public override string Visit(CollectionResourceFunctionCallNode node)
        {
            string result = node.Name;
            if (node.Source != null)
            {
                result = this.TranslatePropertyAccess(node.Source, result);
            }

            return this.TranslateFunctionCall(result, node.Parameters);
        }

        /// <summary>
        /// Translates a <see cref="SingleValueOpenPropertyAccessNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleValueOpenPropertyAccessNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.Name);
        }

        /// <summary>
        /// Translates an <see cref="CollectionOpenPropertyAccessNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(CollectionOpenPropertyAccessNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.Name);
        }

        /// <summary>
        /// Translates a <see cref="SingleValuePropertyAccessNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(SingleValuePropertyAccessNode node)
        {
            return this.TranslatePropertyAccess(node.Source, node.Property.Name);
        }

        /// <summary>
        /// Translates a <see cref="ParameterAliasNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(ParameterAliasNode node)
        {
            return node.Alias;
        }

        /// <summary>
        /// Translates a <see cref="NamedFunctionParameterNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string of NamedFunctionParameterNode.</returns>
        public override string Visit(NamedFunctionParameterNode node)
        {
            return string.Concat(node.Name, Constants.SymbolEqual, this.TranslateNode(node.Value));
        }

        /// <summary>
        /// Translates a <see cref="NamedFunctionParameterNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string of SearchTermNode.</returns>
        public override string Visit(SearchTermNode node)
        {
            return node.Text;
        }

        /// <summary>
        /// Translates a <see cref="UnaryOperatorNode"/> into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>The translated string.</returns>
        public override string Visit(UnaryOperatorNode node)
        {
            string result = null;
            if (node.OperatorKind == UnaryOperatorKind.Negate)
            {
                result = Constants.SymbolNegate;
            }

            // if current translated node is SearchNode, the UnaryOperator should return NOT, or return not
            if (node.OperatorKind == UnaryOperatorKind.Not)
            {
                if (this.searchFlag)
                {
                    result = Constants.SearchKeywordNot;
                }
                else
                {
                    result = Constants.KeywordNot;
                }
            }

            if (node.Operand.Kind == QueryNodeKind.Constant || node.Operand.Kind == QueryNodeKind.SearchTerm)
            {
                return string.Concat(result, ' ', this.TranslateNode(node.Operand));
            }
            else
            {
                return string.Concat(result, Constants.SymbolOpenParen, this.TranslateNode(node.Operand), Constants.SymbolClosedParen);
            }
        }

        /// <summary>Translates a <see cref="LevelsClause"/> into a string.</summary>
        /// <param name="levelsClause">The levels clause to translate.</param>
        /// <returns>The translated string.</returns>
        internal static string TranslateLevelsClause(LevelsClause levelsClause)
        {
            string levelsStr = levelsClause.IsMaxLevel
                ? Constants.KeywordMax
                : levelsClause.Level.ToString(CultureInfo.InvariantCulture);
            return levelsStr;
        }

        /// <summary>
        /// Main dispatching visit method for translating query-nodes into expressions.
        /// </summary>
        /// <param name="node">The node to visit/translate.</param>
        /// <returns>The LINQ string resulting from visiting the node.</returns>
        internal string TranslateNode(QueryNode node)
        {
            return node.Accept(this);
        }

        /// <summary>Translates a <see cref="SearchClause"/> into a <see cref="SearchClause"/>.</summary>
        /// <param name="searchClause">The search clause to translate.</param>
        /// <returns>The translated string.</returns>
        internal string TranslateSearchClause(SearchClause searchClause)
        {
            this.searchFlag = true;
            string searchStr = this.TranslateNode(searchClause.Expression);
            this.searchFlag = false;
            return searchStr;
        }

        /// <summary>
        /// Add dictionary to url and each alias value will be URL encoded.
        /// </summary>
        /// <param name="dictionary">key value pair dictionary</param>
        /// <returns>The url query string of dictionary's key value pairs (URL encoded)</returns>
        internal string TranslateParameterAliasNodes(IDictionary<string, SingleValueNode> dictionary)
        {
            string result = null;
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, SingleValueNode> keyValuePair in dictionary)
                {
                    if (keyValuePair.Value != null)
                    {
                        string tmp = this.TranslateNode(keyValuePair.Value);
                        result = string.IsNullOrEmpty(tmp) ? result : string.Concat(result, string.IsNullOrEmpty(result) ? null : Constants.RequestParamsAggregator.ToString(), keyValuePair.Key, Constants.SymbolEqual, Uri.EscapeDataString(tmp));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Helper for translating an access to a metadata-defined property or navigation.
        /// </summary>
        /// <param name="sourceNode">The source of the property access.</param>
        /// <param name="edmPropertyName">The structural or navigation property being accessed.</param>
        /// <param name="navigationSource">The navigation source of the result, required for navigations.</param>
        /// <returns>The translated string.</returns>
        private string TranslatePropertyAccess(QueryNode sourceNode, string edmPropertyName, IEdmNavigationSource navigationSource = null)
        {
            string source = this.TranslateNode(sourceNode);

            if (string.IsNullOrEmpty(source))
            {
                return this.QueryFormatter.TranslateFieldName(edmPropertyName);
            }
            else
            {
                return this.QueryFormatter.TranslateSource(source, edmPropertyName);
            }
        }

        /// <summary>
        /// Translates a function call into a corresponding <see cref="string"/>.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentNodes">The argument nodes.</param>
        /// <returns>
        /// The translated string.
        /// </returns>
        private string TranslateFunctionCall(string functionName, IEnumerable<QueryNode> argumentNodes)
        {
            string result = string.Empty;
            foreach (QueryNode queryNode in argumentNodes)
            {
                result = string.Concat(result, string.IsNullOrEmpty(result) ? null : Constants.SymbolComma.ToString(), this.TranslateNode(queryNode));
            }

            var translatedFunctionCall = string.Concat(QueryFormatter.TranslateFunctionName(functionName), Constants.SymbolOpenParen, result, Constants.SymbolClosedParen);
            return functionName == Constants.KeywordTrim ? $"{translatedFunctionCall}{Constants.SymbolClosedParen}" : translatedFunctionCall;
        }

        /// <summary>
        /// Build BinaryOperatorNode to uri 
        /// </summary>
        /// <param name="operatorKind">the kind of the BinaryOperatorNode</param>
        /// <returns>string format of the operator</returns>
        private string BinaryOperatorNodeToString(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Equal:
                    return Constants.SQLEqualSymbol;
                case BinaryOperatorKind.NotEqual:
                    return Constants.SQLNotEqualSymbol;
                case BinaryOperatorKind.GreaterThan:
                    return Constants.SQLGreaterThanSymbol;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return Constants.SQLGreaterThanOrEqualSymbol;
                case BinaryOperatorKind.LessThan:
                    return Constants.SQLLessThanSymbol;
                case BinaryOperatorKind.LessThanOrEqual:
                    return Constants.SQLLessThanOrEqualSymbol;
                case BinaryOperatorKind.And:
                    return Constants.SQLAndSymbol;
                case BinaryOperatorKind.Or:
                    return Constants.SQLOrSymbol;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the priority of BinaryOperatorNode
        /// This priority table is from <c>http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part2-url-conventions.html</c> (5.1.1.9 Operator Precedence )
        /// </summary>
        /// <param name="operatorKind">binary operator </param>
        /// <returns>the priority value of the binary operator</returns>
        private int TranslateBinaryOperatorPriority(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Or:
                    return 1;
                case BinaryOperatorKind.And:
                    return 2;
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    return 3;
                case BinaryOperatorKind.Add:
                case BinaryOperatorKind.Subtract:
                    return 4;
                case BinaryOperatorKind.Divide:
                case BinaryOperatorKind.Multiply:
                case BinaryOperatorKind.Modulo:
                    return 5;
                case BinaryOperatorKind.Has:
                    return 6;
                default:
                    return -1;
            }
        }
    }
}
