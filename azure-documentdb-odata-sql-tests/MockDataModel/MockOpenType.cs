using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace azure_documentdb_odata_sql_tests
{
    [DataContract]
    public class MockOpenType : Document
    {
        /// <summary>
        /// The property name for Email
        /// </summary>
        public const string EnglishNamePropertyName = "englishName";

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = EnglishNamePropertyName)]
        [JsonProperty(PropertyName = EnglishNamePropertyName)]
        public string EnglishName { get; set; }

        public Dictionary<string, object> PropertyBags { get; set; }
    }
}
