using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.OData.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Query;
using System.Web.OData.Routing;

namespace azure_documentdb_odata_sql_tests
{
    [TestClass]
    public class ODataToSqlTests
    {
        [TestMethod, TestCategory("CIT")]
        public void BuildODataQueryOptionsTest()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var type = typeof(MockOpenType);
            var entityTypeConfiguration = builder.AddEntityType(type);
            entityTypeConfiguration.HasKey(type.GetProperty("Id"));
            builder.AddEntitySet(type.Name, entityTypeConfiguration);
            var edmModels = builder.GetEdmModel();
            var oDataQueryContext = new ODataQueryContext(edmModels, type, new ODataPath());

            HttpRequestMessage httpRequestMessage;
            ODataQueryOptions oDataQueryOptions;
            
            var odataSqlBuilder = new ODataNodeToStringBuilder(new SQLQueryFormatter());
            var sqlQuery = string.Empty;
            var feedOptions = new FeedOptions();

            httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            httpRequestMessage.RequestUri = new Uri("http://localhost/User?$orderby=companyId desc,id asc");
            oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);
            sqlQuery = odataSqlBuilder.Translate(oDataQueryOptions, "User", ref feedOptions);
            Assert.AreEqual("SELECT * FROM c WHERE c._t = 'USER' ORDER BY c.companyId desc, c.id asc ", sqlQuery);
            Assert.IsNull(feedOptions.MaxItemCount);

            httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            httpRequestMessage.RequestUri = new Uri("http://localhost/Post?$top=10");
            oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);
            sqlQuery = odataSqlBuilder.Translate(oDataQueryOptions, "Post", ref feedOptions);
            Assert.AreEqual("SELECT * FROM c WHERE c._t = 'POST' ", sqlQuery);
            Assert.AreEqual(10, feedOptions.MaxItemCount);

            httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            httpRequestMessage.RequestUri = new Uri("http://localhost/Post?$filter=ownerId eq 'ownerId1'");
            oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);
            sqlQuery = odataSqlBuilder.Translate(oDataQueryOptions, "Post", ref feedOptions);
            Assert.AreEqual("SELECT * FROM c WHERE c._t = 'POST' AND c.ownerId = 'ownerId1' ", sqlQuery);

            httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            httpRequestMessage.RequestUri = new Uri("http://localhost/Post?$filter=title eq 'title1' and property/field ne 'val' or viewedCount ge 5 and likedCount ne 3 or viewedCount le -4&$orderby=_lastClientEditedDateTime asc, createdDateTime desc&$top=30");
            oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, httpRequestMessage);
            sqlQuery = odataSqlBuilder.Translate(oDataQueryOptions, "Post", ref feedOptions);
            Assert.AreEqual("SELECT * FROM c WHERE c._t = 'POST' AND c.title = 'title1' AND c.property.field != 'val' OR c.viewedCount >= 5 AND c.likedCount != 3 OR c.viewedCount <= -4 ORDER BY c._lastClientEditedDateTime asc, c.createdDateTime desc ", sqlQuery);
            Assert.AreEqual(30, feedOptions.MaxItemCount);
        }
    }
}
