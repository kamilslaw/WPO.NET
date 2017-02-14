using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WPO.Attributes;
using WPO.Connection;
using WPO.Exceptions;

namespace WPO.Tests.AttributesTests
{
    [TestClass]
    public class WPOColumnAttributeTests
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
        [ExpectedException(typeof(NotColumnAttributeDefinedException))]
        public void TestPropertyWithoutColumnAttribute()
        {
            TestClass instance = new TestClass(session);
            instance.GetColumnName(nameof(instance.NonDataBaseProperty));
        }

        [TestMethod]
        public void TestPropertiesColumnAttributes()
        {
            TestClass instance = new TestClass(session);

            string id = instance.GetColumnName(nameof(instance.Id));
            string value = instance.GetColumnName(nameof(instance.SomeValue));
            string name = instance.GetColumnName(nameof(instance.Name));

            Assert.AreEqual("testclassid", id);
            Assert.AreEqual("somevalue", value);
            Assert.AreEqual("custom_name", name);
        }

        #endregion Test Methods

        #region Private Classes

        [WPOTable]
        class TestClass : WPOBaseObject
        {
            public TestClass(Session session) : base(session) { }
            [WPOPrimaryKey("testclassid")]
            public int Id { get; set; }
            [WPOColumn]
            public decimal SomeValue { get; set; }
            [WPOColumn("custom_name")]
            public decimal Name { get; set; }

            public int NonDataBaseProperty { get; set; }
        }

        #endregion Private Classes
    }
}
