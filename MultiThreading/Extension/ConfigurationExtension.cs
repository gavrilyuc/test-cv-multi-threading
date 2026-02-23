using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MultiThreading;

internal static class ConfigurationExtension
{
	public static T Configure <T>(this IServiceCollection services,
		IConfiguration configuration,
		string sectionName,
		Func<T, T>? configure = null)
		where T : class, new()
	{
		var section = configuration.GetSection(sectionName);

		var cfg = section.Get<T>()
			?? new T();

		if (configure != null)
		{
			cfg = configure(cfg);
		}

		services.AddSingleton(cfg);

		return cfg;
	}
}