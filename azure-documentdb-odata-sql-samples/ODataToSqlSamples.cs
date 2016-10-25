using System;
using System.Net.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Query;
using System.Web.OData.Routing;

using Microsoft.Azure.Documents.OData.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace azure_documentdb_odata_sql_tests
{
    [TestClass]
    public class ODataToSqlSamples
    {
        /// <summary>
        /// 
        /// </summary>
        private static ODataQueryContext oDataQueryContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private static HttpRequestMessage httpRequestMessage { get; set; }

        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var type = typeof(MockOpenType);
            var entityTypeConfiguration = builder.AddEntityType(type);
            entityTypeConfiguration.HasKey(type.GetProperty("Id"));
            builder.AddEntitySet(type.Name, entityTypeConfiguration);
            var edmModels = builder.GetEdmModel();
            oDataQueryContext = new ODataQueryContext(edmModels, type, new ODataPath());
        }

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
        }

        [TestMethod]
        public void BuildSelectAllSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT * FROM c ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=englishName, id");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT c.englishName, c.id FROM c ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectWithEnumSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=enumNumber, id");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT c.enumNumber, c.id FROM c ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectAllTopSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId desc,id asc&$top=15");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT TOP 15 * FROM c ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectTopSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=p1, p2, p3&$filter=property ne 'str1'&$orderby=companyId desc,id asc&$top=15");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT TOP 15 c.p1, c.p2, c.p3 FROM c ", sqlQuery);
        }

        [TestMethod]
        public void BuildWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.englishName = 'Microsoft' AND c.intField <= 5 ", sqlQuery);
        }

        [TestMethod]
        public void BuildWhereWithEnumSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=enumNumber eq azure_documentdb_odata_sql_tests.MockEnum'ONE' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.enumNumber = 'ONE' AND c.intField <= 5 ", sqlQuery);
        }

        [TestMethod]
        public void BuildWhereWithNextedFieldsSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=parent/child eq 'childValue' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.parent.child = 'childValue' AND c.intField <= 5 ", sqlQuery);
        }

        [TestMethod]
        public void BuildAdditionalWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE, "c.dataType = 'MockOpenType'");
            Assert.AreEqual("WHERE c.dataType = 'MockOpenType' AND c.englishName = 'Microsoft' AND c.intField <= 5 ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE, "c.dataType = 'MockOpenType'");
            Assert.AreEqual("SELECT * FROM c WHERE c.dataType = 'MockOpenType' AND c.englishName = 'Microsoft' ", sqlQuery);
        }

        [TestMethod]
        public void BuildOrderBySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId desc,id asc");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ORDERBY_CLAUSE);
            Assert.AreEqual("ORDER BY c.companyId desc, c.id asc ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectOrderBySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId desc,id asc");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE | TranslateOptions.ORDERBY_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE c.property != 'str1' ORDER BY c.companyId desc, c.id asc ", sqlQuery);
        }

        [TestMethod]
        public void BuildSelectTopWhereOrderBySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/Post?$select=id, englishName&$filter=title eq 'title1' and property/field ne 'val' or viewedCount ge 5 and (likedCount ne 3 or enumNumber eq azure_documentdb_odata_sql_tests.MockEnum'TWO')&$orderby=_lastClientEditedDateTime asc, createdDateTime desc&$top=30");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL, "c._t = 'dataType'");
            Assert.AreEqual("SELECT TOP 30 c.id, c.englishName FROM c WHERE c._t = 'dataType' AND c.title = 'title1' AND c.property.field != 'val' OR c.viewedCount >= 5 AND (c.likedCount != 3 OR c.enumNumber = 'TWO') ORDER BY c._lastClientEditedDateTime asc, c.createdDateTime desc ", sqlQuery);
        }
    }
}
