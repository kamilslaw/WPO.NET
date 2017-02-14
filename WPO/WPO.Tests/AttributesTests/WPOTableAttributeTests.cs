using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WPO.Attributes;
using WPO.Connection;
using WPO.Exceptions;

namespace WPO.Tests.AttributesTests
{
    [TestClass]
    public class WPOTableAttributeTests
    {
        #region Utils

        private static Session session;

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            WPOManager manager = WPOManager.GetInstance();
            var mock = new Mock<IDbConnection>();
            session = manager.GetSession(mock.Object, string.Empty);
        }

        #endregion Utils

        #region Test Methods

        [TestMethod]
        [ExpectedException(typeof(NotTableAttributeDefinedException))]
        public void TestClassWithoutTableAttribute()
        {
            TestClass1 instance = new TestClass1(session);
            instance.GetTableName();
        }

        [TestMethod]
        public void TestTablesNamesAtrributes()
        {
            TestClass2 instance = new TestClass2(session);
            TestClass3 instance2 = new TestClass3(session);

            string name = instance.GetTableName();
            string name2 = instance2.GetTableName();
            
            Assert.AreEqual("testclass2", name);
            Assert.AreEqual("custom_table", name2);
        }

        #endregion Test Methods

        #region Privat Classes
        
        class TestClass1 : WPOBaseObject
        {
            public TestClass1(Session session) : base(session) { }
            [WPOPrimaryKey]
            public int Id { get; set; }
        }

        [WPOTable]
        class TestClass2 : WPOBaseObject
        {
            public TestClass2(Session session) : base(session) { }
            [WPOPrimaryKey]
            public int Id { get; set; }
        }

        [WPOTable("custom_table")]
        class TestClass3 : WPOBaseObject
        {
            public TestClass3(Session session) : base(session) { }
            [WPOPrimaryKey]
            public int Id { get; set; }
        }

        #endregion Privat Classes
    }
}
