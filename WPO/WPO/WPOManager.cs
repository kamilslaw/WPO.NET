using System;
using System.Collections.Generic;
using System.Linq;
using WPO.Connection;

namespace WPO
{
    public class WPOManager : IDisposable
    {
        private Dictionary<string, Session> sessions; // The connection strings are the keys      

        #region Get Instance

        private static class Wrapper
        {
            public static WPOManager Instance = new WPOManager();
        }

        private WPOManager()
        {
        }

        public static WPOManager GetInstance()
        {
            return Wrapper.Instance;
        }

        #endregion Get Instance

        #region Public Properties

        public static WPOConfiguration Configuration { get; set; } = WPOConfiguration.DefaultConfiguration;

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            if (sessions != null && sessions.Any())
            {
                foreach (var session in sessions)
                {
                    session.Value.Dispose();
                }
            }

            sessions = null;
            GC.SuppressFinalize(this);
        }

        public Session GetSession(IDbConnection dbConnection, string connectionString)
        {
            if (dbConnection == null)
            {
                throw new ArgumentNullException(nameof(dbConnection));
            }

            if (sessions == null)
            {
                sessions = new Dictionary<string, Session>();
            }

            Session session = null;
            if (!sessions.ContainsKey(connectionString))
            {
                session = new Session(dbConnection, connectionString);
                sessions.Add(connectionString, session);
            }
            else
            {
                session = sessions[connectionString];
            }

            return session;
        }

        public Query<T> GetQuery<T>(Session session)
            where T : WPOBaseObject
        {
            return new Query<T>(session ?? Session.Empty);
        }

        #endregion Public Methods
    }
}
