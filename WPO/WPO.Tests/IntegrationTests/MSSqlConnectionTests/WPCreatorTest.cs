using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Reflection;
using WPO.Schemas;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class WPCreatorTest : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }    

        [TestMethod]
        public void WPCreatorTableSchemaTest()
        {            
            WPCreator.CreateSchema(session, @"WPO.Tests.Utils", Assembly.GetExecutingAssembly());            
        }
    }
}
