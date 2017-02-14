using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Diagnostics.Contracts;

using static WPO.Tests.Utils.WPOTestClasses;
using System.Collections.Generic;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class BasicQueriesTests : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }

        #region Test Methods
        
        [TestMethod]
        public void GetSingleRowTest()
        {
            Product prod = wpoManager.GetQuery<Product>(session).GetObjectByKey(1);

            Assert.IsNotNull(prod);
            Assert.AreEqual(1, prod.Id);
            Assert.AreEqual("New NAME!", prod.Name);
            Assert.AreEqual("123.23", prod.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void GetMultipleRowTest()
        {
            Brand b2 = new Brand(Session.Empty) { Id = 2, CustomType = 0, IsNew = null, Name = "Brand 1", Date = new DateTime(2016, 12, 30) };
            Brand b3 = new Brand(Session.Empty) { Id = 3, CustomType = 1, IsNew = true, Name = "Brand 2", Date = new DateTime(2015, 12, 30) };
            Brand b4 = new Brand(Session.Empty) { Id = 4, CustomType = 3, IsNew = false, Name = "Brand 3", Date = null };

            var result = wpoManager.GetQuery<Brand>(session).Where("id != 4").ToList();

            Assert.IsTrue(result != null && result.Any());
            Assert.IsTrue(result.Any(b => b.Equals(b2)));
            Assert.IsTrue(result.Any(b => b.Equals(b3)));
            Assert.IsFalse(result.Any(b => b.Equals(b4)));
        }

        [TestMethod]
        public void SkipAndTakeTest()
        {
            List<Product> products = wpoManager.GetQuery<Product>(session).Skip(1).Take(2).ToList();

            Assert.IsNotNull(products);
            Assert.AreEqual(2, products.Count);

            products = wpoManager.GetQuery<Product>(session).Skip(0).Take(1).ToList();

            Assert.IsNotNull(products);
            Assert.AreEqual(1, products.Count);
        }

        [TestMethod]
        public void LazyLoadingBasicTest()
        {
            string phrase = "Product Name";
            List<Product> products = wpoManager.GetQuery<Product>(session)
                                                      .Where("ProductDescription LIKE '" + phrase + "'")
                                                      .Where("ProductName LIKE '" + phrase + "'", true)
                                                      .Skip(5)
                                                      .Take(5)
                                                      .ToList();

            Assert.IsNotNull(products);
        }

        #endregion Test Methods
    }
}
