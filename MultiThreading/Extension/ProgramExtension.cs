using System.Net;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MultiThreading;

internal static class ProgramExtension
{
	extension(IServiceCollection services)
	{
		internal IServiceCollection RegisterDependencies()
		{
			services.AddSingleton<IRobotsTxtParser, RobotsTxtParser>();
			services.AddSingleton<IRobotsTxtService, RobotsTxtService>();
			services.AddSingleton<IHtmlParser, HtmlParser>();
			services.AddSingleton<IWebDownloader, WebDownloader>();

			return services;
		}

		internal IServiceCollection BuildDefaultConfiguration(IConfiguration configuration, string[] args)
		{
			services.Configure<ApplicationConfiguration>(configuration, Constants.ApplicationSectionName, cfg =>
			{
				if (args.Length >= 2 && int.TryParse(args[1], out var md))
				{
					cfg.MaxDepth = md;
				}

				if (args.Length >= 3 && int.TryParse(args[2], out var mc))
				{
					cfg.MaxConcurrency = mc;
				}

				return cfg;
			});

			services.Configure<RobotsConfiguration>(configuration, Constants.RobotsSectionName);

			return services;
		}

		internal IServiceCollection BuildHttpClient(IConfiguration configuration)
		{
			var httpClientCfg = services.Configure<HttpClientConfiguration>(configuration, Constants.HttpClientSectionName);

			services.AddHttpClient(Constants.HttpClientName, client =>
			{
				client.Timeout = TimeSpan.FromSeconds(httpClientCfg.TimeoutSeconds);

				client.DefaultRequestHeaders.UserAgent.ParseAdd(httpClientCfg.UserAgent);
			})
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.All
			});

			return services;
		}
	}

	public static IHost BuildConsoleApplicationHost(this string[] args)
	{
		var host = Host.CreateDefaultBuilder(args);

		host.ConfigureAppConfiguration((_, cfg) =>
		{
			cfg.AddJsonFile(Constants.AppSettingsFileName, optional: true, reloadOnChange: false);
			cfg.AddEnvironmentVariables();
		});

		host.ConfigureServices((context, services) =>
		{
			var config = context.Configuration;

			services.BuildDefaultConfiguration(config, args);

			services.BuildHttpClient(config);

			services.RegisterDependencies();

			services.AddLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddJsonConsole();
				logging.AddConfiguration(config.GetSection(Constants.LoggingSectionName));
			});
		});

		return host.Build();
	}
}