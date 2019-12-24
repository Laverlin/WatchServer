using IB.WatchServer.Service.Infrastructure.Linq2DB;
using LinqToDB.DataProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace IB.WatchServer.Test
{

    public class MockConnectionSettings : IConnectionSettings
    {
        public IDataProvider GetDataProvider()
        {
            throw new System.NotImplementedException();
        }

        public string PropertyOne { get; set; }
        public bool? PropertyTwo { get; set; }
        public int PropertyThree { get; set; }
    }

    public class MockConnectionSettingsWithDisplayName : IConnectionSettings
    {
        public IDataProvider GetDataProvider()
        {
            throw new System.NotImplementedException();
        }

        [DisplayName("property-one")]
        public string PropertyOne { get; set; }
        [DisplayName("property-two")]
        public bool? PropertyTwo { get; set; }
        [DisplayName("property-three")]
        public int PropertyThree { get; set; }
    }

    [TestClass]
    public class Linq2DBInfrastructureTests
    {
        [TestMethod]
        public void GetConnectionStringShouldReturnStringWithAllPropertiesWithValue()
        {
            IConnectionSettings mockSettings = new MockConnectionSettings
            {
                PropertyOne = "one",
                PropertyTwo = true,
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.AreEqual("PropertyOne=one;PropertyTwo=True;PropertyThree=3;", connectionString);
        }

        [TestMethod]
        public void GetConnectionStringShouldReturnStringWhereNullPropertiesIgnored()
        {
            IConnectionSettings mockSettings = new MockConnectionSettings
            {
                PropertyOne = "one",
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.AreEqual("PropertyOne=one;PropertyThree=3;", connectionString);
        }

        [TestMethod]
        public void GetConnectionStringShouldReturnPropertyNameChangedByDisplayName()
        {
            IConnectionSettings mockSettings = new MockConnectionSettingsWithDisplayName
            {
                PropertyOne = "one",
                PropertyTwo = false,
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.AreEqual("property-one=one;property-two=False;property-three=3;", connectionString);
        }

    }
}
