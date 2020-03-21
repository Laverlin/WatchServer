using System.Text.Json;
using IB.WatchServer.Service.Controllers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IB.WatchServer.Test.ControllerTest
{
    [TestClass]
    public class YAFaceControllerTest
    {


        [TestMethod]
        public void LocationShouldReturnUpgradeMessage()
        {
            // Arrange
            //
            var controller = new YAFaceController(null, null, null, null, null);
            
            var expected = new LocationResponse {CityName = "Update required."};
            var expectedJson = JsonSerializer.Serialize(expected);

            // Act
            //
            var result = controller.Location();
            var resultJson = JsonSerializer.Serialize(result.Value);

            // Arrange
            //
            Assert.AreEqual(expected.CityName, result.Value.CityName);
            Assert.AreEqual(expectedJson, resultJson);
        }
    }
}
