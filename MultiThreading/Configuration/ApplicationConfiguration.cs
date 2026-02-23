namespace MultiThreading;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ApplicationConfiguration
{
	public string OutputDir { get; set; } = "output";

	public int MaxDepth { get; set; } = 2;

	public int MaxConcurrency { get; set; } = 10;

	public int MaxLinks { get; set; } = 50000;

	public DownloadMode DownloadMode { get; set; } = DownloadMode.SubDomains;
}