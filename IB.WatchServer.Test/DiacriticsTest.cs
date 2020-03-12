using IB.WatchServer.Service.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IB.WatchServer.Test
{
    [TestClass]
    public class DiacriticsTest
    {
        [TestMethod]
        public void FrenchAcsonsShouldBeRemoved()
        {
            var uCity = "crème brûlée";
            var city = uCity.StripDiacritics();

            Assert.AreEqual("creme brulee", city);
        }

        [TestMethod]
        public void NullShouldReturnNull()
        {
            string uCity = null;
            var city = uCity.StripDiacritics();

            Assert.IsNull(city);
        }
    }
}
