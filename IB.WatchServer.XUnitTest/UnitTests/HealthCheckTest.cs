using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using IB.WatchServer.Abstract;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class HealthCheckTest
    {
        [Fact]
        public void HealthCheckShouldReturnHealthJson()
        {
            // Arrange
            //
            var expectedJson =
                "{\r\n  \"serverVersion\": \"" + SolutionInfo.Version + "\",\r\n  \"status\": \"Healthy\",\r\n  \"totalDuration\": \"00:00:01.1490236\",\r\n  \"results\": {\r\n    \"database\": {\r\n      \"status\": \"Healthy\",\r\n      \"description\": null,\r\n      \"data\": {}\r\n    },\r\n    \"location\": {\r\n      \"status\": \"Healthy\",\r\n      \"description\": null,\r\n      \"data\": {}\r\n    }\r\n  }\r\n}";
            var reports = new Dictionary<string, HealthReportEntry> {
            {
                "database", 
                new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.FromSeconds(1), null, null)
            },
            {
                "location", 
                new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.FromSeconds(1), null, null)
            }
            };

            var report = new HealthReport(
                new ReadOnlyDictionary<string, HealthReportEntry>(reports), TimeSpan.FromMilliseconds(1149.0236));

            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            

            // Act
            //
            HealthCheckExtensions.WriteHealthResultResponse(context, report);

            var code = context.Response.StatusCode;
            context.Response.Body.Position = 0;
            var result = new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEnd();

            //Assert
            //
            Assert.Equal(200, code);
            Assert.Equal(expectedJson, result, true, true, true);

        }
    }
}
