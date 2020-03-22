using IB.WatchServer.Service.Infrastructure;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class DiacriticsTest
    {
        [Fact]
        public void FrenchAcsonsShouldBeRemoved()
        {
            var uCity = "crème brûlée";
            var city = uCity.StripDiacritics();

            Assert.Equal("creme brulee", city);
        }

        [Fact]
        public void NullShouldReturnNull()
        {
            string uCity = null;
            var city = uCity.StripDiacritics();

            Assert.Null(city);
        }
    }
}
