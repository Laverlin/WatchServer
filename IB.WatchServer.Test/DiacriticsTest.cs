using System;
using System.Collections.Generic;
using System.Text;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IB.WatchServer.Test
{
    [TestClass]
    public class DiacriticsTest
    {
        [TestMethod]
        public void FrenchAcsonsShouldBeRemoved()
        {
            var uCity = "Chênex, Haute-Savoie, France";
            var city = uCity.StripDiacritics();

            Assert.AreEqual("Chenex, Haute-Savoie, France", city);
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
