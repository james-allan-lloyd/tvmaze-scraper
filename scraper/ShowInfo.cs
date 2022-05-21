using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace scraper;

public class ShowInfo
{
	[BsonId]
	public int id { get; set; }
	public string? name { get; set; }

	[JsonIgnore]
	public List<PersonInfo> cast = new();
}