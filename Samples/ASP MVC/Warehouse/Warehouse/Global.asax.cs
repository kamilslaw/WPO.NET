using System.Web.Mvc;
using System.Web.Routing;
using Warehouse.App_Start;

namespace Warehouse
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            DBManager.Initialize();
        }
    }
}
