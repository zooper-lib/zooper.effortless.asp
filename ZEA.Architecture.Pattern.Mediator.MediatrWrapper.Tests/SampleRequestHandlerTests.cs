using Xunit;
using ZEA.Architecture.Pattern.Mediator.MediatrWrapper.Tests.Samples;

namespace ZEA.Architecture.Pattern.Mediator.MediatrWrapper.Tests;

public class SampleRequestHandlerTests
{
	[Fact]
	public async Task HandleAsync_ReturnsExpectedResult()
	{
		// Arrange
		var handler = new SampleRequestHandler();
		var request = new SampleRequest
		{
			Data = "Test data"
		};

		// Act
		var result = await handler.HandleAsync(request, CancellationToken.None);

		// Assert
		Assert.Equal("Processed data: Test data", result);
	}
}