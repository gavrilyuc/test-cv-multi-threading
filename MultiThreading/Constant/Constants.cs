using System.Net.Mime;

namespace MultiThreading;

internal static class Constants
{
	public const string AppSettingsFileName = "appsettings.json";

	public const string ApplicationSectionName = "Application";
	public const string RobotsSectionName = "Robots";
	public const string HttpClientSectionName = "HttpClient";
	public const string LoggingSectionName = "Logging";

	public const string HttpClientName = "MultiThreadingBot";

	public const string CommaDelimiter = ",";
	public static readonly string[] QuotesDelimiter = [ "\'", "\"" ];

	public const char WebUrlDelimiterChar = '/';
	public const string HtmlContentType = "html";
	public const string UnknownFileExtension = ".bin";
	public const string RootFileName = "index.html";
	public static readonly string[] HtmlFileExtensions = [ ".html", ".htm" ];

	public const char FileSnakeDelimiter = '_'; 

	public const string HtmlImgTag = "img";
	public const string HtmlSrcAttribute = "src";
	public const string HtmlScriptTag = "script";
	public const string HtmlATag = "a";
	public const string HtmlStyleSelector = "link[rel~='stylesheet']";
	public const string HtmlHrefAttribute = "href";

	private const string MediaTypeApplicationJavaScript = "application/javascript";
	private const string MediaTypeApplicationXHtml = "application/xhtml+xml";

	public const string Dot = ".";
	public const char UriSchemaDelimiter = ':';
	public const string HttpQueryStart = "//";
	public const string HttpsWithSchemaDelimiter = "https://";

	public static readonly Dictionary<string, string> UnsupportedWebFileNameSymbols = new()
	{
		{ "?", "_" },
		{ ":", "_" },
		{ "*", "_" },
		{ "\"", "_" },
		{ "<", "_" },
		{ ">", "_" },
		{ "|", "_" },
	};

	public static readonly Dictionary<string, string> MediaTypeToFileExtension = new()
	{
		{ MediaTypeNames.Image.Jpeg, ".jpg" },
		{ MediaTypeNames.Image.Png, ".png" },
		{ MediaTypeNames.Image.Bmp, ".bmp" },
		{ MediaTypeNames.Image.Gif, ".gif" },
		{ MediaTypeNames.Image.Webp, ".webp" },
		{ MediaTypeNames.Image.Icon, ".icon" },
		{ MediaTypeNames.Image.Svg, ".svg" },
		{ MediaTypeNames.Image.Tiff, ".tiff" },
		{ MediaTypeNames.Text.Css, ".css" },
		{ MediaTypeNames.Text.JavaScript, ".js" },
		{ MediaTypeApplicationJavaScript, ".js" },
		{ MediaTypeApplicationXHtml, ".html" },
		{ MediaTypeNames.Application.Json, ".json" },
		{ MediaTypeNames.Application.Xml, ".xml" },
		{ MediaTypeNames.Application.JsonPatch, ".json" },
		{ MediaTypeNames.Application.Pdf, ".pdf" },
		{ MediaTypeNames.Application.Octet, ".octet" },
		{ MediaTypeNames.Application.Soap, ".xml" },
		{ MediaTypeNames.Application.Zip, ".zip" },
		{ MediaTypeNames.Application.Yaml, ".yaml" },
		{ MediaTypeNames.Application.XmlDtd, ".dtd" },
		{ MediaTypeNames.Application.ProblemXml, ".xml" },
		{ MediaTypeNames.Application.XmlPatch, ".xml" },
		{ MediaTypeNames.Application.Rtf, ".rtf" },
		{ MediaTypeNames.Application.GZip, ".zip" },
		{ MediaTypeNames.Application.Wasm, ".wasm" },
		{ MediaTypeNames.Text.Html, ".html" },
		{ MediaTypeNames.Text.Xml, ".xml" },
		{ MediaTypeNames.Text.Csv, ".csv" },
		{ MediaTypeNames.Text.Plain, ".txt" },
		{ MediaTypeNames.Text.Markdown, ".md" },
		{ MediaTypeNames.Text.Rtf, ".rtf" },
	};
}