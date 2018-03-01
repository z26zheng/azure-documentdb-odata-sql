# Microsoft.Azure.Documents.OData.Sql

Converts [OData V4](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html) queries to [DocumentDB SQL](https://azure.microsoft.com/en-us/documentation/articles/documentdb-sql-query/) queries. 

## Summary

This package supports most of the intersectional functionalities provided by OData V4 and DocumentDB SQL. For example, if you have a class looks like:
```
public class Company {
  public string englishName,
  public string countryCode,
  public int    revenue
}
```
To query all companies whose englishName contains "Limited", and sort them by countryCode in descending order, then select the revenue property from top 5 results, you can issue an OData query to your web service:
```
http://localhost/Company?$select=revenue&$filter=contains(englishName, 'Limited')&$orderby=countryCode desc&$top=5
```
The above query will then be translated to DocumentDB SQL:
```
SELECT TOP 5 c.revenue FROM c WHERE CONTAINS(c.englishName,'Limited') ORDER BY c.countryCode DESC 
```
Note: requires  Microsoft.AspNet.OData 6.1.0.0 and .NET Framework 4.62

### Supported OData to DocumentDB SQL mappings:

#### System Query Options::
[$select](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_3) => SELECT

[$filter](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_The_$filter_System) => WHERE

[$top](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_The_$top_System_1) => TOP

[$orderby](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_The_$select_System_1) => ORDER BY

[$count](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_The_$inlinecount_System) => COUNT(1)

### Built-in Operators
Items/any(d:d/Quantity gt 100)  => JOIN a in c.Items WHERE a.Quantity > 100
Note: If more objects in 'Items' qualify for the expression, duplicate results may result. e.g.
SELECT  value c FROM c
JOIN a IN c.sub
WHERE a.v=false  might return c twice, while c exists once, because the join in 'sub' has two hits

#### Built-in Query Functions

contains()(field, 'value')	 => CONTAINS(c.field, 'value')

startswith()(field, 'value') => STARTSWITH(c.field, 'value')

endswith()(field, 'value')	 => ENDSWITH(c.field, 'value')

toupper()(field, 'value')    => UPPER(c.field, 'value')

tolower()(field, 'value')    => LOWER(c.field, 'value')

length()(field)              => LENGTH(c.field)

indexof(field,'value')       => INDEX_OF(c.field,'value')
          
substring(field,idx1,idx2)   => SUBSTRING(c.field,idx1,idx2)
 
trim(field)                  => LTRIM(RTRIM(c.englishName))

concat(field,'value')        => CONCAT(c.englishName,'value')

geo.distance(field, geography'POINT(30 10)') => ST_DISTANCE(c.location,{"type":"Point","coordinates":[30,10]})

geo.intersects(field, geography'POLYGON((30 10, 10 20, 20 40, 40 40, 30 10))') => ST_INTERSECTS(c.area,{"type":"Polygon","coordinates":[[[30,10],[10.20],[20,40],[40,40],[30,10]]]})

## Installing

The nuget package of this project is published on Nuget.org [Download Page](https://www.nuget.org/packages/Microsoft.Azure.Documents.OData.Sql/). To install in Visual Studio Package Manager Console, run command: 
```
PM> Install-Package Microsoft.Azure.Documents.OData.Sql
```

## Usage

After installation, you can include the binary in your \*.cs file by
```
using Microsoft.Azure.Documents.OData.Sql;
```
-You can find a complete set of examles in [ODataToSqlSamples.cs](https://github.com/z26zheng/azure-documentdb-odata-sql/blob/master/azure-documentdb-odata-sql-samples/ODataToSqlSamples.cs)-

There are only two classes you need to work with in order to get the translation done: [SQLQueryFormatter](https://github.com/z26zheng/azure-documentdb-odata-sql/blob/master/azure-documentdb-odata-sql/ODataToSqlTranslator/SqlQueryFormatter.cs) and [ODataToSqlTranslator](https://github.com/z26zheng/azure-documentdb-odata-sql/blob/master/azure-documentdb-odata-sql/ODataToSqlTranslator/ODataToSqlTranslator.cs). 
#### SQLQueryFormatter
SQLQueryFormatter is where we do the property and function name translation, such as translating 'propertyName' to 'c.propertyName', or 'contains()' to 'CONTAINS()'. This class inherits from [QueryFormatterBase](https://github.com/z26zheng/azure-documentdb-odata-sql/blob/master/azure-documentdb-odata-sql/ODataToSqlTranslator/QueryFormatterBase.cs), which abstractly defines all required translations. One can derive from QueryFormatterBase to implement their own translation per need.

#### ODataToSqlTranslator
ODataToSqlTranslator does the overall translation, by taking in an implementation of QueryFormatterBase. Because the specific translation is defined in QueryFormatter, the translation performs differently according to the implementation in QueryFormatter. 
The default QueryFormatter is SQLQueryFormatter mentioned above:
```
var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
```
Once we have an instance of ODataToSqlTranslator, we can call .Translate(ODataQueryOptions, TranslateOptions, string) method:
```
var translatedSQL = oDataToSqlTranslator.(oDataQueryOptions, TranslateOptions.ALL, additionalWhereClause: null);
```
In the above example, the [ODataQueryOptions](https://msdn.microsoft.com/en-us/library/system.web.http.odata.query.odataqueryoptions(v=vs.118).aspx) is a fundamental class provided by ASP.NET [Web API 2](https://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/supporting-odata-query-options), there you can find detailed instruction on ODataQueryOptions. Additionally, you can also find ODataQueryOptions usage in the ODataToSqlSamples file. 
The second parameter is TranslateOptions, which defines what queries to be translated. Define TranslateOptions are:
```
SELECT_CLAUSE, WHERE_CLAUSE, ORDERBY_CLAUSE, TOP_CLAUSE, ALL
```
The options can be combined with bit operators such as ```(TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE)```. One common usage is ```(TranslateOptions.ALL & ~TranslateOptions.TOP)```, this combination enables all translation but TOP. The reason to disable TOP is that when performing pagination, DocumentDB ignores [continuation token in FeedOptions](https://msdn.microsoft.com/en-us/library/microsoft.azure.documents.client.feedoptions.requestcontinuation.aspx) if TOP exists. Therefore, the best practice is to use ```FeedOptions``` to perform TOP operation in DocumentDB.


## Authors

* **Ziyou Zheng** - Microsoft Universal Store Team -
* **Egbert Nierop** - Free Lance developer - Added any functionality 2017 oct 13. note: all-functionality not supported.
* **ntanaka** - Added $count, geography'POINT and geography'POLYGON( translations