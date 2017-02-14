using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using WPO.Helpers;

namespace WPO.Connection
{
    internal class InsertTransactionStrategy : ITransactionStrategy
    {
        public void MakeTransaction(IEnumerable<ExecuteCommand> commands, SqlConnection connection)
        {
            //INSERTING RECORDS
            foreach (var command in commands)
            {
                var standardColumns = command.Objects.Where(o => !o.Value.Item2 && o.Value.Item1 != null);
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO ");
                sb.Append(command.TableName);
                sb.Append("(");
                foreach (var column in standardColumns)
                {
                    if (!column.Equals(standardColumns.First()))
                    {
                        sb.Append(", ");
                    }

                    sb.Append(column.Key);
                }

                sb.Append(") VALUES(");

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                int i = 0;
                foreach (var column in standardColumns)
                {
                    if (!column.Equals(standardColumns.First()))
                    {
                        sb.Append(", ");
                    }

                    sb.Append("@param");
                    sb.Append(i);

                    cmd.Parameters.AddWithValue("@param" + i, column.Value.Item1);
                    i++;
                }

                sb.Append(");");
                string query = sb.ToString();
                query.WriteDiagnostics();

                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }

            //UPDATING TO ADD RELATIONS
            foreach (var command in commands)
            {
                var relationColumns = command.Objects.Where(o => o.Value.Item2 && o.Value.Item1 != null);
                if (relationColumns.Any())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("UPDATE ");
                    sb.Append(command.TableName);
                    sb.Append(" SET ");

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    int i = 0;
                    foreach (var column in relationColumns)
                    {
                        if (!column.Equals(relationColumns.First()))
                        {
                            sb.Append(", ");
                        }

                        sb.Append(column.Key);
                        sb.Append("=");
                        sb.Append("@param");
                        sb.Append(i);

                        cmd.Parameters.AddWithValue("@param" + i, column.Value.Item1);
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
}
