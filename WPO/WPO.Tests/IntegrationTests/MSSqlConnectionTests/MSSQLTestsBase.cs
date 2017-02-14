using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPO.Connection;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class MSSQLTestsBase
    {
        protected static MSSqlConnection connection;
        protected static string connectionString = "Data Source=mssql4.gear.host;Initial Catalog=wpo;User id=wpo;Password=Ax6DmK-50_nS;";
        //protected static string connectionString = @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=master;";
        protected static WPOManager wpoManager;
        protected static Session session, session2, session3;

        protected static void TestsInitialization()
        {
            if (connection != null)
            {
                connection.Dispose();
            }

            connection = new MSSqlConnection();
            connection.ConnectionString = connectionString;

            wpoManager = WPOManager.GetInstance();
            session = wpoManager.GetSession(connection, connectionString);
            session2 = wpoManager.GetSession(connection, connectionString + "  ");
            session3 = wpoManager.GetSession(connection, connectionString + "   ");
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            wpoManager.Dispose();
        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            connection.Open();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            connection.Close();
        }
    }
}
