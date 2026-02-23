using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MultiThreading.UnitTests;

public abstract class MultiThreadingUnitTest : IDisposable
{
	protected readonly ServiceProvider Services;
	protected readonly IConfiguration Configuration;

	protected MultiThreadingUnitTest()
	{
		var sc = new ServiceCollection();

		Configuration = new ConfigurationBuilder()
			.AddJsonFile(Constants.AppSettingsFileName, true, false)
			.Build();

		sc.BuildDefaultConfiguration(Configuration, []);
		sc.BuildHttpClient(Configuration);
		sc.BuildHttpClient(Configuration);
		sc.RegisterDependencies();

		// ReSharper disable once VirtualMemberCallInConstructor
		var f = BuildFakeClientFactory();
		if (f != null)
		{
			sc.AddSingleton(f);
		}

		sc.AddSingleton<RobotsTxtParser>();
		sc.AddSingleton<RobotsTxtService>();
		sc.AddSingleton<WebDownloader>();

		sc.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConsole();
		});

		Services = sc.BuildServiceProvider();
	}

	public virtual void Dispose()
	{
		Services?.Dispose();
	}

	protected virtual IHttpClientFactory? BuildFakeClientFactory()
	{
		return null;
	}
}