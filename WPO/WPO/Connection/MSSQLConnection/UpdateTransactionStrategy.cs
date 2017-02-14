using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using WPO.Helpers;

namespace WPO.Connection
{
    internal class UpdateTransactionStrategy : ITransactionStrategy
    {
        public void MakeTransaction(IEnumerable<ExecuteCommand> commands, SqlConnection connection)
        {
            foreach (var command in commands)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("UPDATE ");
                sb.Append(command.TableName);
                sb.Append(" SET ");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                int i = 0;
                foreach (var column in command.Objects)
                {
                    if (!column.Equals(command.Objects.First()))
                    {
                        sb.Append(", ");
                    }

                    sb.Append(column.Key);
                    sb.Append("=");
                    if (column.Value.Item1 != null)
                    {
                        sb.Append("@param");
                        sb.Append(i);

                        cmd.Parameters.AddWithValue("@param" + i, column.Value.Item1);
                    }
                    else
                    {
                        sb.Append("NULL");
                    }

                    i++;
                }

                sb.Append(" WHERE ");
                sb.Append(command.ObjectsIdentifiers.Single().GetPrimaryKeyQueryString());
                sb.Append(";");

                string query = sb.ToString();
                query.WriteDiagnostics();

                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
