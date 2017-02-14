using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using WPO.Helpers;

namespace WPO.Connection
{
    internal class DeleteTransactionStrategy : ITransactionStrategy
    {
        public void MakeTransaction(IEnumerable<ExecuteCommand> commands, SqlConnection connection)
        {
            foreach (var command in commands)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("DELETE FROM ");
                sb.Append(command.TableName);
                sb.Append(" WHERE ");
                foreach (var primaryKey in command.ObjectsIdentifiers)
                {
                    if (!primaryKey.Equals(command.ObjectsIdentifiers.First()))
                    {
                        sb.Append(" OR ");
                    }

                    sb.Append(primaryKey.GetPrimaryKeyQueryString());
                }

                sb.Append(";");
                string query = sb.ToString();
                query.WriteDiagnostics();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
