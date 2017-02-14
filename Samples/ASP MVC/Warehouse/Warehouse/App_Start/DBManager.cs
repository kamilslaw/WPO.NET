using WPO;
using WPO.Connection;

namespace Warehouse.App_Start
{
    public static class DBManager
    {
        private static MSSqlConnection connection;
        private static string connectionString = "Data Source=mssql4.gear.host;Initial Catalog=wpo;User id=wpo;Password=Ax6DmK-50_nS;";
        //private static string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Kamilslaw\\Studia\\SEMESTR V\\Wzorce\\Projekt\\Samples\\ASP MVC\\Warehouse\\Warehouse\\App_Data\\Database1.mdf\";Integrated Security=True";

        public static WPOManager Manager { get; set; }

        public static Session Session { get; set; }

        public static void Initialize()
        {
            connection = new MSSqlConnection();

            Manager = WPOManager.GetInstance();
            Session = Manager.GetSession(connection, connectionString);
        }
    }
}