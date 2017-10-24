using System;
using System.Net.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Query;
using System.Web.OData.Routing;

using Microsoft.Azure.Documents.OData.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.OData.Extensions;

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
        public static void ClassInitialize(TestContext testContext)
        {
            var builder = new ODataConventionModelBuilder();
            var type = typeof(MockOpenType);
            var entityTypeConfiguration = builder.AddEntityType(type);
            entityTypeConfiguration.HasKey(type.GetProperty("Id"));
            builder.AddEntitySet(type.Name, entityTypeConfiguration);
            var edmModels = builder.GetEdmModel();
            oDataQueryContext = new ODataQueryContext(edmModels, type, new ODataPath());
        }

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void TestInitialize()
        {
            httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get
            };
            var config = new System.Web.Http.HttpConfiguration();
            config.EnableDependencyInjection();
            httpRequestMessage.SetConfiguration(config);
        }

        [TestMethod]
        public void TranslateSelectAllSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT * FROM c ", sqlQuery);
        }

        [TestMethod]
        public void TranslateSelectSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=englishName, id");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT c.englishName, c.id FROM c ", sqlQuery);
        }

        [TestMethod]
        public void TranslateSelectWithEnumSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=enumNumber, id");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE);
            Assert.AreEqual("SELECT c.enumNumber, c.id FROM c ", sqlQuery);
        }
        [TestMethod]
        public void TranslateAnySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=companies/any(p: p/id eq 'abc' or p/name eq 'blaat')");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("SELECT * FROM c JOIN a IN c.companies WHERE a.id = 'abc' OR a.name = 'blaat'", sqlQuery);
        }
        [TestMethod]
        public void TranslateAnySampleWithMultipleClauses()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=(companies/any(p: p/id eq 'abc' or p/name eq 'blaat')) and customers/any(x: x/customer_name eq 'jaap')");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("SELECT * FROM c JOIN a IN c.companies JOIN b IN c.customers WHERE a.id = 'abc' OR a.name = 'blaat' AND b.customer_name = 'jaap'", sqlQuery);
        }
        [TestMethod]
        public void TranslateSelectAllTopSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId DESC,id ASC&$top=15");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT TOP 15 * FROM c ", sqlQuery);
        }

        [TestMethod]
        public void TranslateSelectTopSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$select=p1, p2, p3&$filter=property ne 'str1'&$orderby=companyId DESC,id ASC&$top=15");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT TOP 15 c.p1, c.p2, c.p3 FROM c ", sqlQuery);
        }

        [TestMethod]
        public void TranslateWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.englishName = 'Microsoft' AND c.intField <= 5", sqlQuery);
        }

        [TestMethod]
        public void TranslateWhereSampleWithGUID()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=id eq 2ED27DF5-F505-4A06-B168-7321C6B4AD0C");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.id = '2ed27df5-f505-4a06-b168-7321c6b4ad0c'", sqlQuery);
        }

        [TestMethod]
        public void TranslateWhereWithEnumSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=enumNumber eq azure_documentdb_odata_sql_tests.MockEnum'ONE' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.enumNumber = 'ONE' AND c.intField <= 5", sqlQuery);
        }

        [TestMethod]
        public void TranslateWhereWithNextedFieldsSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=parent/child eq 'childValue' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE);
            Assert.AreEqual("WHERE c.parent.child = 'childValue' AND c.intField <= 5", sqlQuery);
        }

        [TestMethod]
        public void TranslateAdditionalWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft' and intField le 5");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.WHERE_CLAUSE, "c.dataType = 'MockOpenType'");
            Assert.AreEqual("WHERE c.dataType = 'MockOpenType' AND c.englishName = 'Microsoft' AND c.intField <= 5", sqlQuery);
        }

        [TestMethod]
        public void TranslateSelectWhereSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost?$filter=englishName eq 'Microsoft'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE, "c.dataType = 'MockOpenType'");
            Assert.AreEqual("SELECT * FROM c WHERE c.dataType = 'MockOpenType' AND c.englishName = 'Microsoft'", sqlQuery);
        }

        [TestMethod]
        public void TranslateOrderBySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId desc,id asc");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ORDERBY_CLAUSE);
            Assert.AreEqual("ORDER BY c.companyId DESC, c.id ASC ", sqlQuery);
        }

        [TestMethod]
        public void TranslateSelectOrderBySample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=property ne 'str1'&$orderby=companyId desc,id asc");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE c.property != 'str1' ORDER BY c.companyId DESC, c.id ASC ", sqlQuery);
        }

        [TestMethod]
        public void TranslateContainsSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=contains(englishName, 'Microsoft')");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE CONTAINS(c.englishName,'Microsoft')", sqlQuery);
        }

        [TestMethod]
        public void TranslateStartswithSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=startswith(englishName, 'Microsoft')");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE STARTSWITH(c.englishName,'Microsoft')", sqlQuery);
        }

        [TestMethod]
        public void TranslateEndswithSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=endswith(englishName, 'Microsoft')");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE ENDSWITH(c.englishName,'Microsoft')", sqlQuery);
        }

        [TestMethod]
        public void TranslateUpperAndLowerSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=toupper(englishName) eq 'MICROSOFT' or tolower(englishName) eq 'microsoft'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE UPPER(c.englishName) = 'MICROSOFT' OR LOWER(c.englishName) = 'microsoft'", sqlQuery);
        }

        [TestMethod]
        public void TranslateLengthSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=length(englishName) ge 10 and length(englishName) lt 15");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE LENGTH(c.englishName) >= 10 AND LENGTH(c.englishName) < 15", sqlQuery);
        }

        [TestMethod]
        public void TranslateIndexOfSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=indexof(englishName,'soft') eq 4");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE INDEX_OF(c.englishName,'soft') = 4", sqlQuery);
        }

        [TestMethod]
        public void TranslateSubstringSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=substring(englishName, 1, length(englishName)) eq 'icrosoft'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE SUBSTRING(c.englishName,1,LENGTH(c.englishName)) = 'icrosoft'", sqlQuery);
        }

        [TestMethod]
        public void TranslateTrimSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=trim(englishName) eq 'Microsoft'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE LTRIM(RTRIM(c.englishName)) = 'Microsoft'", sqlQuery);
        }

        [TestMethod]
        public void TranslateConcatSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$filter=concat(englishName, ' Canada') eq 'Microsoft Canada'");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
            Assert.AreEqual("SELECT * FROM c WHERE CONCAT(c.englishName,' Canada') = 'Microsoft Canada'", sqlQuery);
        }

        [TestMethod]
        public void TranslateMasterSample()
        {
            httpRequestMessage.RequestUri = new Uri("http://localhost/Post?$select=id, englishName&$filter=title eq 'title1' and property/field ne 'val' or viewedCount ge 5 and (likedCount ne 3 or enumNumber eq azure_documentdb_odata_sql_tests.MockEnum'TWO')&$orderby=_lastClientEditedDateTime asc, createdDateTime desc&$top=30");
            var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);

            var oDataToSqlTranslator = new ODataToSqlTranslator(new SQLQueryFormatter());
            var sqlQuery = oDataToSqlTranslator.Translate(oDataQueryOptions, TranslateOptions.ALL, "c._t = 'dataType'");
            Assert.AreEqual("SELECT TOP 30 c.id, c.englishName FROM c WHERE c._t = 'dataType' AND c.title = 'title1' AND c.property.field != 'val' OR c.viewedCount >= 5 AND (c.likedCount != 3 OR c.enumNumber = 'TWO') ORDER BY c._lastClientEditedDateTime ASC, c.createdDateTime DESC ", sqlQuery);
        }
    }
}
