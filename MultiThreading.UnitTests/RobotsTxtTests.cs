using Microsoft.Extensions.DependencyInjection;

namespace MultiThreading.UnitTests;

public sealed class RobotsTxtTests : MultiThreadingUnitTest
{
	[Fact]
	public void RobotsTxtParser_Parse()
	{
		// Arrange
		var parser = Services.GetRequiredService<RobotsTxtParser>();

		var content = @"
				# sample robots
				User-Agent: *
				Disallow: /private
				Allow: /private/public
			";

		// Act
		var robots = parser.Parse(content, "any-agent");

		// Assert
		Assert.Contains("/private/public", robots.Allow);
		Assert.Contains("/private", robots.Disallow);

		// Arrange
		content = "User-agent: *\nDisallow:";

		// Act
		robots = parser.Parse(content, "any-agent");

		// Assert
		Assert.True(robots.IsAllowed("/private/public"));
		Assert.True(robots.IsAllowed("/private"));
	}

	[Fact]
	public void RobotsTxt_IsAllowed()
	{
		// Arrange
		var robots = new RobotsTxt(["/a/b"], ["/a"]);

		// Act & Assert
		Assert.True(robots.IsAllowed("/a/b/c"));
		Assert.False(robots.IsAllowed("/a/c"));
	}
}