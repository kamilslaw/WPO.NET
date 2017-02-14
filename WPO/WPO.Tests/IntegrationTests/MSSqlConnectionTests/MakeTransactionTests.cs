using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Diagnostics.Contracts;

using static WPO.Tests.Utils.WPOTestClasses;
using System;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class MakeTransactionTests : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }

        #region Test Method

        [TestMethod]
        public void BasicInsertAndDeleteTest()
        {
            session.Rollback();
            int id = 907867;
            Product product = new Product(session) { Id = id, Name = "Product Name", Price = 44.44m, ProductDescription = "Product Great Description!" };
            
            session.Commit();

            Product productFromBase = wpoManager.GetQuery<Product>(session2).Single(p => p.Id == id);
            productFromBase.Remove();

            session2.Commit();

            Product productFromBaseAfterDeleting = wpoManager.GetQuery<Product>(session2).FirstOrDefault(p => p.Id == id);

            Assert.AreEqual(product.Name, productFromBase.Name);
            Assert.AreEqual(product.Price, productFromBase.Price);
            Assert.AreEqual(product.ProductDescription, productFromBase.ProductDescription);

            Assert.AreEqual(null, productFromBaseAfterDeleting);
        }

        [TestMethod]
        public void BasicUpdateTest()
        {
            Product product = wpoManager.GetQuery<Product>(session2).GetObjectByKey(2);
            string originalName = product.Name;
            decimal originalPrice = product.Price;
            product.Name = "Second Product New Name!";
            product.Price += 10;
            session2.Commit();

            Product changedProduct = wpoManager.GetQuery<Product>(session3).GetObjectByKey(2);
            string changedName = changedProduct.Name;
            decimal changedPrice = changedProduct.Price;
            changedProduct.Name = originalName;
            changedProduct.Price -= 10;
            session3.Commit();

            Product rechangedProduct = wpoManager.GetQuery<Product>(session).GetObjectByKey(2);

            Assert.AreEqual("Second Product New Name!", changedName);
            Assert.AreEqual(originalName, rechangedProduct.Name);
            Assert.AreEqual(originalPrice + 10, changedPrice);
            Assert.AreEqual(originalPrice, changedProduct.Price);
        }

        [TestMethod]
        public void InsertingWithSequenceTest()
        {
            session.Rollback();
            long theBiggestId = wpoManager.GetQuery<Brand>(session).OrderByDescending(b => b.Id).First().Id;

            Brand brand = new Brand(session) { Name = "Next brand", IsNew = true, Date = DateTime.Now };
            session.Commit();

            long newTheBiggestId = wpoManager.GetQuery<Brand>(session2).OrderByDescending(b => b.Id).First().Id;

            Assert.IsTrue(newTheBiggestId > theBiggestId);

            brand.Remove();
            session.Commit();
        }

        [TestMethod]
        public void InsertingWithRelationsTest()
        {
            WPOManager.Configuration = WPOConfiguration.DefaultConfiguration;
            session.Rollback();
            session2.Rollback();
            Random random = new Random();

            Simple1 s1 = new Simple1(session);
            s1.Id = random.Next();
            s1.Name = "My number is " + s1.Id;
            Simple2 s2 = new Simple2(session);
            s2.Id = s1.Id + 1;
            s2.Parent = s1;
            Simple3 s3_1 = new Simple3(session);
            s3_1.Id = s2.Id + 1;
            s3_1.Parent = s2;
            Simple3 s3_2 = new Simple3(session);
            s3_2.Id = s2.Id + 2;
            s3_2.Parent = s2;

            session.Commit();
            
            Simple3 _s3_1 = wpoManager.GetQuery<Simple3>(session2).GetObjectByKey(s3_1.Id);            

            Assert.IsNotNull(_s3_1);
            Assert.IsNotNull(_s3_1.Parent);

            _s3_1.Parent.LoadDependencies();

            Assert.IsNotNull(_s3_1.Parent.Children3);
            Assert.AreEqual(2, _s3_1.Parent.Children3.Count);
            Assert.AreEqual(s2, _s3_1.Parent);
            Assert.AreEqual(s1, _s3_1.Parent.Parent);
            Assert.AreEqual(4, _s3_1.Session.objectsAll.Count);
        }

        [TestMethod]
        public void UpdatingWithRelationTest()
        {
            WPOManager.Configuration = WPOConfiguration.DefaultConfiguration;
            session.Rollback();
            session2.Rollback();
            session3.Rollback();
            Random random = new Random();

            Simple1 s1_1 = new Simple1(session);
            s1_1.Id = random.Next();
            s1_1.Name = "My number is " + s1_1.Id;
            Simple1 s1_2 = new Simple1(session);
            s1_2.Id = random.Next();
            s1_2.Name = "My number is " + s1_2.Id;
            Simple2 s2 = new Simple2(session);
            s2.Id = s1_1.Id + 1;
            s2.Parent = s1_1;

            session.Commit();

            Simple2 _s2 = wpoManager.GetQuery<Simple2>(session2).GetObjectByKey(s2.Id);
            Simple1 _s1_2 = wpoManager.GetQuery<Simple1>(session2).GetObjectByKey(s1_2.Id);

            Assert.AreEqual(s1_1, _s2.Parent);

            _s2.Parent = _s1_2;

            session2.Commit();

            Simple2 __s2 = wpoManager.GetQuery<Simple2>(session3).GetObjectByKey(s2.Id);

            Assert.AreEqual(s1_2, __s2.Parent);
            
            __s2.Parent = null;
            session3.Commit();

            Simple2 ___s2 = wpoManager.GetQuery<Simple2>(session).GetObjectByKey(s2.Id);

            Assert.AreEqual(null, ___s2.Parent);
        }

        #endregion Test Methods
    }
}
