using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WPO.Attributes;
using WPO.Connection;
using WPO.Exceptions;

namespace WPO.Tests.AttributesTests
{
    [TestClass]
    public class WPOPrimaryKeyAttributeTests
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
        [ExpectedException(typeof(NotPrimaryKeyDefinedException))]
        public void TestNotPrimaryKey()
        {
            ClassWithoutPrimaryKey instance = new ClassWithoutPrimaryKey(session);
            instance.GetPrimaryKey();
        }

        [TestMethod]
        public void TestSinglePrimaryKey()
        {
            ClassWithPrimaryKey instance = new ClassWithPrimaryKey(session) { Id = 44 };

            PrimaryKey result = instance.GetPrimaryKey();

            Assert.IsNotNull(result);
            Assert.AreEqual("id", result.ColumnName);
            Assert.AreEqual(44, result.Value);
            Assert.AreEqual("seq", result.SequenceName);
        }

        #endregion Test Methods

        #region Private Classes

        [WPOTable]
        class ClassWithoutPrimaryKey : WPOBaseObject
        {
            public ClassWithoutPrimaryKey(Session session) : base(session) { }
            public int Id { get; set; }
            [WPOColumn]
            public decimal Value { get; set; }
        }

        [WPOTable]
        class ClassWithPrimaryKey : WPOBaseObject
        {
            public ClassWithPrimaryKey(Session session) : base(session) { }
            [WPOPrimaryKey("id", true, "seq")]
            public int Id { get; set; }
            [WPOColumn]
            public decimal Value { get; set; }
        }

        #endregion Private Classes
    }
}
