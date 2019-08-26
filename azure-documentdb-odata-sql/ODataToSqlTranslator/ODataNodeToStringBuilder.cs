using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

// ReSharper disable once CheckNamespace
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
		private bool _searchFlag;

		/// <summary>s
		/// Gets the formatter to format the query
		/// </summary>
		private QueryFormatterBase QueryFormatter { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ODataNodeToStringBuilder"/> class
		/// </summary>
		/// <param name="queryFormatter">the query format class</param>
		public ODataNodeToStringBuilder(QueryFormatterBase queryFormatter)
		{
			QueryFormatter = queryFormatter;
		}

		/// <summary>
		/// Prevents a default instance of the <see cref="ODataNodeToStringBuilder"/> class from being created
		/// </summary>
		// ReSharper disable once UnusedMember.Local
		private ODataNodeToStringBuilder()
		{
		}

		/// <inheritdoc />
		public override string Visit(AllNode node)
		{
			string result = string.Concat(TranslateNode(node.Source), Constants.SymbolForwardSlash, Constants.KeywordAll, Constants.SymbolOpenParen, node.CurrentRangeVariable.Name, Constants.SymbolColon, TranslateNode(node.Body), Constants.SymbolClosedParen);
			return result;
		}

		/// <inheritdoc />
		public override string Visit(AnyNode node)
		{
			if (node.CurrentRangeVariable == null && node.Body.Kind == QueryNodeKind.Constant)
			{
				return string.Concat(TranslateNode(node.Source), Constants.SymbolForwardSlash, Constants.KeywordAny, Constants.SymbolOpenParen, Constants.SymbolClosedParen);
			}
			else
			{
				var source = TranslateNode(node.Source);
				var body = TranslateNode(node.Body);
				var variableName = node.CurrentRangeVariable?.Name;

				var result = string.Concat(source, Constants.SymbolForwardSlash, Constants.KeywordAny,
					Constants.SymbolOpenParen, variableName, ":", body,
					Constants.SymbolClosedParen);

				return result;
			}
		}

		/// <inheritdoc />
		public override string Visit(BinaryOperatorNode node)
		{
			var leftNode = node.Left;
			while (leftNode != null && leftNode.Kind == QueryNodeKind.Convert)
			{
				leftNode = ((ConvertNode)leftNode).Source;
			}

			var rightNode = node.Right;
			while (rightNode != null && rightNode.Kind == QueryNodeKind.Convert)
			{
				rightNode = ((ConvertNode)rightNode).Source;
			}

			var left = TranslateNode(node.Left);
			if (leftNode?.Kind == QueryNodeKind.BinaryOperator && TranslateBinaryOperatorPriority(((BinaryOperatorNode)leftNode).OperatorKind) < TranslateBinaryOperatorPriority(node.OperatorKind))
			{
				left = string.Concat(Constants.SymbolOpenParen, left, Constants.SymbolClosedParen);
			}

			var right = TranslateNode(node.Right);
			if (rightNode?.Kind == QueryNodeKind.BinaryOperator && TranslateBinaryOperatorPriority(((BinaryOperatorNode)rightNode).OperatorKind) < TranslateBinaryOperatorPriority(node.OperatorKind))
			{
				right = string.Concat(Constants.SymbolOpenParen, right, Constants.SymbolClosedParen);
			}

			return string.Concat(left, Constants.SymbolSpace, BinaryOperatorNodeToString(node.OperatorKind), Constants.SymbolSpace, right);
		}

		/// <inheritdoc />
		public override string Visit(CollectionNavigationNode node)
		{
			var navigationPath = GetNavigationPath(node);
			if (navigationPath.StartsWith($"{Constants.SQLFieldNameSymbol}{Constants.SymbolDot}"))
			{
				return $"{Constants.SQLJoinSymbol} x {Constants.SQLInSymbol} {navigationPath}";
			}
			return $"{Constants.SQLJoinSymbol} x {Constants.SQLInSymbol} {Constants.SQLFieldNameSymbol}{Constants.SymbolDot}{navigationPath}";
		}

		/// <inheritdoc />
		public override string Visit(CollectionPropertyAccessNode node)
		{
			return TranslatePropertyAccess(node.Source, node.Property.Name);
		}

		/// <inheritdoc />
		public override string Visit(ConstantNode node)
		{
			if (node.Value == null)
			{
				return Constants.KeywordNull;
			}

			if (node.TypeReference.Definition.TypeKind == EdmTypeKind.Enum)
			{
				return QueryFormatter.TranslateEnumValue(node.LiteralText, ((ODataEnumValue)node.Value).TypeName);
			}

			if (node.TypeReference.IsDateTimeOffset())
			{
				return $"'{node.LiteralText}'";
			}

			return node.LiteralText;
		}

		/// <inheritdoc />
		public override string Visit(ConvertNode node)
		{
			return TranslateNode(node.Source);
		}

		/// <inheritdoc />
		public override string Visit(CollectionResourceCastNode node)
		{
			return TranslatePropertyAccess(node.Source, node.CollectionType.Definition.ToString());
		}

		/// <inheritdoc />
		public override string Visit(ResourceRangeVariableReferenceNode node)
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

		/// <inheritdoc />
		public override string Visit(NonResourceRangeVariableReferenceNode node)
		{
			return node.Name;
		}

		/// <inheritdoc />
		public override string Visit(SingleComplexNode node)
		{
			return GetNavigationPath(node);
		}

		/// <inheritdoc />
		public override string Visit(SingleResourceCastNode node)
		{
			return TranslatePropertyAccess(node.Source, node.TypeReference.Definition.ToString());
		}

		/// <inheritdoc />
		public override string Visit(SingleValueCastNode node)
		{
			return TranslatePropertyAccess(node.Source, node.TypeReference.Definition.ToString());
		}

		/// <inheritdoc />
		public override string Visit(SingleNavigationNode node)
		{
			return TranslatePropertyAccess(node.Source, node.NavigationProperty.Name);
		}

		/// <inheritdoc />
		public override string Visit(SingleResourceFunctionCallNode node)
		{
			string result = node.Name;
			if (node.Source != null)
			{
				result = TranslatePropertyAccess(node.Source, result);
			}

			return TranslateFunctionCall(result, node.Parameters);
		}

		/// <inheritdoc />
		public override string Visit(SingleValueFunctionCallNode node)
		{
			string result = node.Name;
			if (node.Source != null)
			{
				result = TranslatePropertyAccess(node.Source, result);
			}

			return TranslateFunctionCall(result, node.Parameters);
		}

		/// <inheritdoc />
		public override string Visit(CollectionFunctionCallNode node)
		{
			var result = node.Name;
			if (node.Source != null)
			{
				result = TranslatePropertyAccess(node.Source, result);
			}

			return TranslateFunctionCall(result, node.Parameters);
		}

		/// <inheritdoc />
		public override string Visit(CollectionResourceFunctionCallNode node)
		{
			string result = node.Name;
			if (node.Source != null)
			{
				result = TranslatePropertyAccess(node.Source, result);
			}

			return TranslateFunctionCall(result, node.Parameters);
		}

		/// <inheritdoc />
		public override string Visit(SingleValueOpenPropertyAccessNode node)
		{
			return TranslatePropertyAccess(node.Source, node.Name);
		}

		/// <inheritdoc />
		public override string Visit(CollectionOpenPropertyAccessNode node)
		{
			return TranslatePropertyAccess(node.Source, node.Name);
		}

		/// <inheritdoc />
		public override string Visit(SingleValuePropertyAccessNode node)
		{
			return TranslatePropertyAccess(node.Source, node.Property.Name);
		}

		/// <inheritdoc />
		public override string Visit(ParameterAliasNode node)
		{
			return node.Alias;
		}

		/// <inheritdoc />
		public override string Visit(NamedFunctionParameterNode node)
		{
			return string.Concat(node.Name, Constants.SymbolEqual, TranslateNode(node.Value));
		}

		/// <inheritdoc />
		public override string Visit(SearchTermNode node)
		{
			return node.Text;
		}

		/// <inheritdoc />
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
				result = _searchFlag ? Constants.SearchKeywordNot : Constants.KeywordNot;
			}

			if (node.Operand.Kind == QueryNodeKind.Constant || node.Operand.Kind == QueryNodeKind.SearchTerm)
			{
				return string.Concat(result, ' ', TranslateNode(node.Operand));
			}
			else
			{
				return string.Concat(result, Constants.SymbolOpenParen, TranslateNode(node.Operand), Constants.SymbolClosedParen);
			}
		}

		/// <inheritdoc />
		public override string Visit(CollectionComplexNode nodeIn)
		{
			var navigationPath = GetNavigationPath(nodeIn);
			return $"{Constants.SQLJoinSymbol} x {Constants.SQLInSymbol} {Constants.SQLFieldNameSymbol}{Constants.SymbolDot}{navigationPath}{nodeIn.Property.Name}";
		}

		/// <summary>Translates a <see cref="LevelsClause"/> into a string.</summary>
		/// <param name="levelsClause">The levels clause to translate.</param>
		/// <returns>The translated string.</returns>
		internal static string TranslateLevelsClause(LevelsClause levelsClause)
		{
			var levelsStr = levelsClause.IsMaxLevel
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
			_searchFlag = true;
			string searchStr = TranslateNode(searchClause.Expression);
			_searchFlag = false;
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
						string tmp = TranslateNode(keyValuePair.Value);
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
		/// <returns>The translated string.</returns>
		private string TranslatePropertyAccess(QueryNode sourceNode, string edmPropertyName)
		{
			var source = TranslateNode(sourceNode);

			return string.IsNullOrEmpty(source) ? QueryFormatter.TranslateFieldName(edmPropertyName) : QueryFormatter.TranslateSource(source, edmPropertyName);
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
			var result = string.Empty;
			foreach (QueryNode queryNode in argumentNodes)
			{
				result = string.Concat(result, string.IsNullOrEmpty(result) ? null : Constants.SymbolComma.ToString(), TranslateNode(queryNode));
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
		private static int TranslateBinaryOperatorPriority(BinaryOperatorKind operatorKind)
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

		private static string GetNavigationPath(CollectionResourceNode nodeIn)
		{
			if (nodeIn.NavigationSource == null)
				return string.Empty;

			var pathSegments = nodeIn.NavigationSource.Path.PathSegments.Skip(1).ToArray();
			var path = string.Join(Constants.SymbolDot, pathSegments);

			return string.IsNullOrWhiteSpace(path) ? string.Empty : $"{path}{Constants.SymbolDot}";
		}

		private static string GetNavigationPath(CollectionNavigationNode nodeIn)
		{
			if (nodeIn.NavigationSource == null)
				return nodeIn.NavigationProperty.Name;

			string[] pathSegments;
			if (nodeIn.Source.Kind == QueryNodeKind.SingleComplexNode)
			{
				var paths = GetPathFromSource(nodeIn.Source as SingleComplexNode);
				paths.Add(nodeIn.NavigationProperty.Name);
				pathSegments = paths.ToArray();
			}
			else
			{
				pathSegments = nodeIn.NavigationSource.Path.PathSegments.Skip(1).ToArray();
			}

			var path = string.Join(Constants.SymbolDot, pathSegments);

			return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
		}

		private static string GetNavigationPath(SingleComplexNode nodeIn)
		{
			if (nodeIn.NavigationSource == null)
				return nodeIn.Property.Name;

			string[] pathSegments;
			if (nodeIn.Source.Kind == QueryNodeKind.SingleComplexNode)
			{
				var paths = GetPathFromSource(nodeIn.Source as SingleComplexNode);
				paths.Add(nodeIn.Property.Name);
				pathSegments = paths.ToArray();
			}
			else if (nodeIn.Source.Kind == QueryNodeKind.ResourceRangeVariableReference)
			{
				var paths = GetPathFromSource(nodeIn.Source as ResourceRangeVariableReferenceNode);
				paths.Add(nodeIn.Property.Name);
				pathSegments = paths.ToArray();
			}
			else
			{
				if (nodeIn.NavigationSource.Path.PathSegments.ToArray().Length > 1)
					pathSegments = nodeIn.NavigationSource.Path.PathSegments.Skip(1).ToArray();
				else
					pathSegments = new[] { nodeIn.Property.Name };
			}

			var path = string.Join(Constants.SymbolDot, pathSegments);

			return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
		}

		private static List<string> GetPathFromSource(SingleComplexNode node)
		{
			if (node.Source is ResourceRangeVariableReferenceNode referenceNode)
			{
				var path = GetPathFromSource(referenceNode);
				path.Add(node.Property.Name);
				return path;
			}

			if (!(node.Source is SingleComplexNode))
			{
				return new List<string> { node.Property.Name };
			}

			var sources = GetPathFromSource((SingleComplexNode)node.Source);
			sources.Add(node.Property.Name);
			return sources;
		}

		private static List<string> GetPathFromSource(ResourceRangeVariableReferenceNode node)
		{
			return new List<string> {node.Name == "$it" ? "c" : new ODataNodeToStringBuilder().TranslateNode(node)};
		}
	}
}
