using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AdvancedLogging.Extensions;

namespace AdvancedLogging.UnitTests.Extensions
{
    public class HttpClientExtensionsTests
    {
        [Fact]
        public async Task GetAsync_Should_Retry_On_Failure()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpRequestException("Simulated network error"), 3);
            var client = new HttpClient(handler);
            var retries = 2;

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.GetAsync("http://test.com", retries, 10)
            );

            Assert.Equal(retries + 1, handler.RequestCount);
        }

        [Fact]
        public async Task GetAsync_Should_Succeed_On_First_Try()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var client = new HttpClient(handler);

            // Act
            var response = await client.GetAsync("http://test.com", 3, 10);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task GetAsync_Should_Succeed_After_Retries()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK), 2);
            var client = new HttpClient(handler);

            // Act
            var response = await client.GetAsync("http://test.com", 3, 10);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, handler.RequestCount);
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly Exception _exception;
        private readonly int _throwExceptionUntilRequest;

        public int RequestCount { get; private set; }

        public MockHttpMessageHandler(HttpResponseMessage response, int throwExceptionUntilRequest = 0)
        {
            _response = response;
            _throwExceptionUntilRequest = throwExceptionUntilRequest;
        }

        public MockHttpMessageHandler(Exception exception, int throwExceptionUntilRequest = 1)
        {
            _exception = exception;
            _throwExceptionUntilRequest = throwExceptionUntilRequest;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (RequestCount <= _throwExceptionUntilRequest && _exception != null)
            {
                throw _exception;
            }

            if (_response != null)
            {
                return Task.FromResult(_response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
