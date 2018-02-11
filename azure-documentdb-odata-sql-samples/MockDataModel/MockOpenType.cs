using System.Collections.Generic;
using System.Runtime.Serialization;

using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace azure_documentdb_odata_sql_tests
{
	[DataContract]
	public enum MockEnum
	{
		[EnumMember]
		ZERO,

		[EnumMember]
		ONE,

		[EnumMember]
		TWO
	}


	[DataContract]
	public class MockOpenType : Document
	{
		/// <summary>
		/// The property name for EnglishName
		/// </summary>
		public const string EnglishNamePropertyName = "englishName";

		/// <summary>
		/// The property name for EnumNumber
		/// </summary>
		public const string EnumNumberPropertyName = "enumNumber";

		public const string TagsPropertyName = "tags";
		public const string PointsPropertyName = "points";
		public const string EnumNumbersPropertyName = "enumNumbers";
		public const string ProductsPropertyName = "products";
		public const string LocationsPropertyName = "locations";

		/// <summary>
		/// EnglishName
		/// </summary>
		[DataMember(Name = EnglishNamePropertyName)]
		[JsonProperty(PropertyName = EnglishNamePropertyName)]
		public string EnglishName { get; set; }

		/// <summary>
		/// EnumNumber
		/// </summary>
		[DataMember(Name = EnumNumberPropertyName)]
		[JsonProperty(PropertyName = EnumNumberPropertyName)]
		public MockEnum EnumNumber { get; set; }

		/// <summary>
		/// PropertyBag to make Edm open-typed
		/// </summary>
		public Dictionary<string, object> PropertyBag { get; set; }

		[DataMember(Name = TagsPropertyName)]
		[JsonProperty(PropertyName = TagsPropertyName)]
		public List<string> Tags { get; set; }

		[DataMember(Name = PointsPropertyName)]
		[JsonProperty(PropertyName = PointsPropertyName)]
		public List<int> Points { get; set; }

		[DataMember(Name = EnumNumbersPropertyName)]
		[JsonProperty(PropertyName = EnumNumbersPropertyName)]
		public List<MockEnum> EnumNumbers { get; set; }

		[DataMember(Name = ProductsPropertyName)]
		[JsonProperty(PropertyName = ProductsPropertyName)]
		public List<Product> Products { get; set; }


		[DataMember(Name = LocationsPropertyName)]
		[JsonProperty(PropertyName = LocationsPropertyName)]
		public List<Location> Locations { get; set; }
	}

	[DataContract]
	public class Product
	{
		public const string NamePropertyName = "name";
		public const string PricePropertyName = "price";
		public const string ShippedPropertyName = "shipped";

		[DataMember(Name = NamePropertyName)]
		[JsonProperty(PropertyName = NamePropertyName)]
		public string Name { get; set; }

		[DataMember(Name = PricePropertyName)]
		[JsonProperty(PropertyName = PricePropertyName)]
		public int Price { get; set; }


		[DataMember(Name = ShippedPropertyName)]
		[JsonProperty(PropertyName = ShippedPropertyName)]
		public bool Shipped { get; set; }
	}

	[DataContract]
	public class Location
	{
		public const string NamePropertyName = "name";

		[DataMember(Name = NamePropertyName)]
		[JsonProperty(PropertyName = NamePropertyName)]
		public string Name { get; set; }
	}
}
