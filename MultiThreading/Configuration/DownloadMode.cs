using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MultiThreading;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DownloadMode
{
	[EnumMember(Value = "domain")]
	Domain = 0,

	[EnumMember(Value = "subDomains")]
	SubDomains,

	[EnumMember(Value = "any")]
	Any
}