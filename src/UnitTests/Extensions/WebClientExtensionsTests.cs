using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using AdvancedLogging.Extensions;

namespace AdvancedLogging.UnitTests.Extensions
{
    public class WebClientExtensionsTests
    {
        [Fact]
        public void DownloadString_Should_Retry_On_Failure()
        {
            // Arrange
            var client = new MockWebClient(new WebException("Simulated network error"), 3);
            var retries = 2;

            // Act & Assert
            Assert.Throws<WebException>(() =>
                client.DownloadString("http://test.com", retries, 10)
            );

            Assert.Equal(retries + 1, client.RequestCount);
        }

        [Fact]
        public void DownloadString_Should_Succeed_On_First_Try()
        {
            // Arrange
            var client = new MockWebClient("Success");

            // Act
            var response = client.DownloadString("http://test.com", 3, 10);

            // Assert
            Assert.Equal("Success", response);
            Assert.Equal(1, client.RequestCount);
        }

        [Fact]
        public void DownloadString_Should_Succeed_After_Retries()
        {
            // Arrange
            var client = new MockWebClient("Success", 2);

            // Act
            var response = client.DownloadString("http://test.com", 3, 10);

            // Assert
            Assert.Equal("Success", response);
            Assert.Equal(3, client.RequestCount);
        }
    }

    public class MockWebClient : WebClient
    {
        private readonly string _response;
        private readonly Exception _exception;
        private readonly int _throwExceptionUntilRequest;

        public int RequestCount { get; private set; }

        public MockWebClient(string response, int throwExceptionUntilRequest = 0)
        {
            _response = response;
            _throwExceptionUntilRequest = throwExceptionUntilRequest;
        }

        public MockWebClient(Exception exception, int throwExceptionUntilRequest = 1)
        {
            _exception = exception;
            _throwExceptionUntilRequest = throwExceptionUntilRequest;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            return base.GetWebRequest(address);
        }

        public new string DownloadString(string address)
        {
            RequestCount++;

            if (RequestCount <= _throwExceptionUntilRequest && _exception != null)
            {
                throw _exception;
            }

            if (_response != null)
            {
                return _response;
            }

            throw new WebException("Simulated network error");
        }

        public new byte[] DownloadData(string address)
        {
            return Encoding.UTF8.GetBytes(DownloadString(address));
        }
    }
}
