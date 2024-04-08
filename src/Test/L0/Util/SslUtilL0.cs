using Microsoft.VisualStudio.Services.Agent.Util;
using System.Net.Http;
using Xunit;

namespace Test.L0.Util
{
    public sealed class SslUtilL0
    {
        [Fact]
        public void AddRequestMessageLog_RequestMessageIsNull_ShouldReturnCorrectLog()
        {
            // Arrange
            HttpRequestMessage requestMessage = null;
            var logBuilder = new SslDiagnosticsLogBuilder();

            // Act
            logBuilder.AddRequestMessageLog(requestMessage);
            var log = logBuilder.BuildLog();

            // Assert
            Assert.Equal("Request message is null.", log);
        }

        [Fact]
        public void AddRequestMessageLog_RequestMessageIsNotNull_ShouldReturnCorrectLog()
        {
            // Arrange
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            var logBuilder = new SslDiagnosticsLogBuilder();
            var log = string.Empty;

            // Act
            using (requestMessage)
            {
                logBuilder.AddRequestMessageLog(requestMessage);
                log = logBuilder.BuildLog();
            }

            // Assert
            Assert.Contains("Request message:", log);
            Assert.Contains("Method: GET", log);
            Assert.Contains("Request URI: http://localhost/", log);
        }
    }
}
