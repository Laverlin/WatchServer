using System;
using System.Collections.Generic;
using System.Text;
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
            YAFaceProvider yaFaceProvider = new YAFaceProvider(null, null, null, null);

            var uCity = "Chênex, Haute-Savoie, France";
            var city = yaFaceProvider.RemoveDiacritics(uCity);

            Assert.AreEqual("Chenex, Haute-Savoie, France", city);
        }
    }
}
