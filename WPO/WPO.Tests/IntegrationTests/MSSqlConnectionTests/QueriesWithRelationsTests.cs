using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Diagnostics.Contracts;

using static WPO.Tests.Utils.WPOTestClasses;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class QueriesWithRelationsTests : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }

        #region Test Methods

        [TestMethod]
        public void GetObjectsWithSingleRelationTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = -1 };
            var result = wpoManager.GetQuery<Simple2>(session).ToList();

            Assert.IsTrue(result != null && result.Any());
            Assert.IsTrue(result.Any(s2 => s2.Id == 1 && s2.Parent != null && s2.Parent.Id == 2));
            Assert.IsTrue(result.Any(s2 => s2.Id == 2 && s2.Parent != null && s2.Parent.Id == 1));
        }

        [TestMethod]
        public void GetObjecstWithDoubleRelationTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = -1 };
            var result = wpoManager.GetQuery<Simple3>(session).ToList();

            Assert.IsTrue(result != null && result.Any());
            Assert.IsTrue(result.Any(s3 => s3.Id == 1 && s3.Parent != null && s3.Parent.Id == 1 && s3.Parent.Parent != null && s3.Parent.Parent.Id == 2));
        }

        [TestMethod]
        public void GetObjectsWithLoopRelationTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = -1 };
            var result = wpoManager.GetQuery<Simple4>(session).ToList();

            Assert.IsTrue(result != null && result.Any());
            //Test relations
            Assert.IsFalse(result.Any(s4 => s4.Parent == null));
            Assert.IsTrue(result.Any(s4 => s4.Id == 1 && s4.Other != null && s4.Other.Id == 3 && s4.Other.Other != null && s4.Other.Other.Id == 2));
            Assert.IsTrue(result.Any(s4 => s4.Id == 2 && s4.Other != null && s4.Other.Id == 1 && s4.Other.Other != null && s4.Other.Other.Id == 3));
            Assert.IsTrue(result.Any(s4 => s4.Id == 3 && s4.Other != null && s4.Other.Id == 2 && s4.Other.Other != null && s4.Other.Other.Id == 1));
            //Test relation loop
            Assert.IsTrue(result.Any(s4 => s4.Id == 1 && s4.Other.Other.Other != null && s4.Other.Other.Other.Id == 1));
            //Test WPOCollection
            Assert.IsTrue(result.Any(s4 => s4.Id == 2 && s4.Parent.Children4 != null && s4.Parent.Children4.Any(c => c.Id == 2) && s4.Parent.Children4.Any(c => c.Id == 3)));
            Assert.IsTrue(result.Any(s4 => s4.Id == 1 && s4.Parent.Children4 != null && s4.Parent.Children4.Any(c => c.Id == 1)));
        }

        [TestMethod]
        public void GetObjectsWithOneToOneRelationTest()
        {
            var result = wpoManager.GetQuery<Simple5>(session).ToList();

            Assert.IsTrue(result != null && result.Any());
            Assert.IsTrue(result.Any(s5 => s5.Id == 1 && s5.Other != null && s5.Other.Id == 3));
            Assert.IsTrue(result.Any(s5 => s5.Id == 2 && s5.Other != null && s5.Other.Id == 1));
            Assert.IsTrue(result.Any(s5 => s5.Id == 3 && s5.Other != null && s5.Other.Id == 2));
        }

        #endregion Test Methods
    }
}

