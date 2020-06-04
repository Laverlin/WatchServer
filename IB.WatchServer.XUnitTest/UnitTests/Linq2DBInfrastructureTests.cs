using System.ComponentModel;
using IB.WatchServer.Abstract.Settings;
using IB.WatchServer.Service.Entity.Settings;
using LinqToDB.DataProvider;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
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

    public class Linq2DBInfrastructureTests
    {
        [Fact]
        public void GetConnectionStringShouldReturnStringWithAllPropertiesWithValue()
        {
            IConnectionSettings mockSettings = new MockConnectionSettings
            {
                PropertyOne = "one",
                PropertyTwo = true,
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.Equal("PropertyOne=one;PropertyTwo=True;PropertyThree=3;", connectionString);
        }

        [Fact]
        public void GetConnectionStringShouldReturnStringWhereNullPropertiesIgnored()
        {
            IConnectionSettings mockSettings = new MockConnectionSettings
            {
                PropertyOne = "one",
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.Equal("PropertyOne=one;PropertyThree=3;", connectionString);
        }

        [Fact]
        public void GetConnectionStringShouldReturnPropertyNameChangedByDisplayName()
        {
            IConnectionSettings mockSettings = new MockConnectionSettingsWithDisplayName
            {
                PropertyOne = "one",
                PropertyTwo = false,
                PropertyThree = 3
            };

            var connectionString = mockSettings.BuildConnectionString();

            Assert.Equal("property-one=one;property-two=False;property-three=3;", connectionString);
        }

    }
}
