using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using WPO.Enums;
using WPO.Helpers;
using WPO.Schemas;
using WPO.Connection.MSSQLConnection;
using System.Diagnostics;

namespace WPO.Connection
{
    public class MSSqlConnection : IDbConnection
    {
        private SqlConnection connection;
        private ITransactionStrategy transactionStrategy;

        public string ConnectionString { get; set; }

        #region Public Methods

        public void Open()
        {
            if (connection != null)
            {
                connection.Close();
            }

            connection = new SqlConnection(ConnectionString);
            connection.Open();
        }

        public void Close()
        {
            connection.Close();            
        }

        public void Dispose()
        {
            Close();
            connection.Dispose();
            connection = null;
        }

        public IEnumerable<ExecuteResult<long>> GetSequences(Dictionary<string, int> sequences)
        {
            return ExecuteTask(() =>
            {
                List<ExecuteResult<long>> result = new List<ExecuteResult<long>>();
                int offset = 1;
                while (sequences.Values.Any(val => val >= offset))
                {
                    StringBuilder sb = new StringBuilder("SELECT ");
                    var usingSequences = sequences.Where(s => s.Value >= offset);
                    foreach (var sequence in usingSequences)
                    {
                        sb.Append("NEXT VALUE FOR ");
                        sb.Append(sequence.Key);
                        sb.Append(!usingSequences.Last().Equals(sequence) ? ", " : ";");
                    }

                    string query = sb.ToString();
                    query.WriteDiagnostics();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = query;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        DataTable schema = reader.GetSchemaTable();
                        int pos = 0;
                        foreach (var sequence in usingSequences)
                        {
                            long sequenceValue = reader.GetInt64(pos);
                            result.Add(new ExecuteResult<long>(sequence.Key, sequenceValue));
                            pos++;
                        }
                    }

                    offset++;
                }

                return result;
            });
        }

        public void MakeTransaction(IEnumerable<ExecuteCommand> commands)
        {
            if (commands.Any())
            {
                SetTransactionStrategy(commands.FirstOrDefault());
                ExecuteTask(() =>
                {
                    transactionStrategy.MakeTransaction(commands, connection);
                    return true;
                });
            }
        }
        
        public IEnumerable<Dictionary<string, string>> ExecuteReader(QueryFilter filter)
        {
            return ExecuteTask(() =>
            {
                string query = CreateQuery(filter);
                query.WriteDiagnostics();
                List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
                SqlCommand cmd = new SqlCommand(query);
                cmd.Connection = connection;
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Dictionary<string, string> rowObject = new Dictionary<string, string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        rowObject.Add(reader.GetName(i), reader.GetValue(i).ToString());
                    }

                    result.Add(rowObject);
                }

                reader.Close();
                return result;
            });
        }

        public void CreateSchema(List<TableSchema> schema)
        {                  
            ExecuteTask(() =>
            {
                List<TableSchema> alters = new List<TableSchema>();
                foreach (TableSchema table in schema)
                {
                    if (!IsTableExists(table.Name))
                    {
                        StringBuilder query = new StringBuilder();
                        query.Append("CREATE TABLE " + table.Name + " ( \n");
                        foreach (TableSchema.ColumnSchema column in table.Columns)
                        {
                            query.Append(column.Name);
                            query.Append(" " + DatatypeResolver.Resolve(column.ColType));                
                            if((column.Length != null) && (column.Length > 0))
                            {
                                query.Append("(" + column.Length);
                                if((column.FractionalPartLength != null) && (column.FractionalPartLength > 0))
                                {
                                   query.Append(", " + column.FractionalPartLength);
                                }
                                query.Append(")");
                            }
                            else
                            {
                                query.Append(Converter.EnumToDefaultString(column.ColType));
                            }                         
                            if (!column.AllowNull)
                            {
                                query.Append(" NOT NULL");
                            }
                            if (column.IsPrimaryKey)
                            {
                                query.Append(" PRIMARY KEY");
                            }
                            query.Append(",\n");
                            if (column.HasForeignKey && !alters.Contains(table))
                            {
                                alters.Add(table);
                            }
                        }
                        query.Append(")\n");                        
                        TransactionExtensions.WriteDiagnostics(query.ToString());
                        SqlCommand command = new SqlCommand(query.ToString(), connection);
                        command.ExecuteNonQuery();
                    }
                }

                foreach (TableSchema table in alters)
                {
                    foreach (TableSchema.ColumnSchema column in table.Columns)
                    {
                        if (column.HasForeignKey)
                        {
                            string alterquery = "ALTER TABLE " + table.Name + " \nADD FOREIGN KEY (" + column.Name + ") \nREFERENCES " + column.ForeignTableName + "(" + column.ForeignKeyName + ")\n";
                            TransactionExtensions.WriteDiagnostics(alterquery);
                            SqlCommand command = new SqlCommand(alterquery, connection);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                return true;
            });
        }

        #endregion Public Methods

        #region Private Methods

        private void SetTransactionStrategy(ExecuteCommand executeCommand)
        {
            if (executeCommand != null)
            {
                switch (executeCommand.Type)
                {
                    case ExecuteCommand.CommandType.Delete:
                        transactionStrategy = new DeleteTransactionStrategy();
                        break;
                    case ExecuteCommand.CommandType.Insert:
                        transactionStrategy = new InsertTransactionStrategy();
                        break;
                    case ExecuteCommand.CommandType.Update:
                        transactionStrategy = new UpdateTransactionStrategy();
                        break;
                }
            }
        }

        private T ExecuteTask<T>(Func<T> func)
        {
            bool isNotOpenedBefore = false;
            if (connection.State != ConnectionState.Open)
            {
                isNotOpenedBefore = true;
                Open();
            }

            //Execute func and get result
            T result = func();

            if (isNotOpenedBefore)
            {
                Close();
                isNotOpenedBefore = false;
            }

            return result;
        }

        private string CreateQuery(QueryFilter filter)
        {
            StringBuilder sb = new StringBuilder("SELECT * FROM ");
            sb.Append(filter.Table.TableName);

            //JOIN CLAUSE FOR CLASS TABLE INHERITANCE
            if (filter.Table.Inheritance == InheritanceType.ClassTable)
            {
                WPOTableObject tableObj = filter.Table;
                while (tableObj.BaseTable != null)
                {
                    sb.Append(" JOIN ");
                    sb.Append(tableObj.BaseTable.TableName);
                    sb.Append(" ON ");
                    sb.Append(tableObj.TableName);
                    sb.Append(".");
                    sb.Append(tableObj.PrimaryKey.ColumnName);
                    sb.Append(" = ");
                    sb.Append(tableObj.BaseTable.TableName);
                    sb.Append(".");
                    sb.Append(tableObj.BaseTable.PrimaryKey.ColumnName);

                    tableObj = tableObj.BaseTable;
                }
            }

            //WHERE CLAUSE
            string whereString = string.Empty;
            QueryFilter.Statement.Operator prevOpearator = QueryFilter.Statement.Operator.UNDEFINED;
            while (filter.Conditions != null)
            {
                if (string.IsNullOrEmpty(whereString))
                {
                    whereString = filter.Conditions.Value;
                }
                else
                {
                    whereString = "(" + whereString + ") " +
                                     (prevOpearator == QueryFilter.Statement.Operator.OR ? "OR" : "AND") +
                                     " (" + filter.Conditions.Value + ")";
                }

                prevOpearator = filter.Conditions.LogicalOperator;
                filter.Conditions = filter.Conditions.NextStatement;
            }

            if (!string.IsNullOrEmpty(whereString))
            {
                sb.Append(" WHERE ");
                sb.Append(whereString);
            }

            //ORDER CLAUSE
            string orderString = string.Empty;
            while (filter.Orders != null)
            {
                if (!string.IsNullOrEmpty(orderString))
                {
                    orderString += ", ";
                }

                orderString += filter.Orders.ColumnName + (filter.Orders.OrderingType == QueryFilter.Order.Direction.ASC ? " ASC" : " DESC");

                filter.Orders = filter.Orders.NextOrder;
            }

            if (!string.IsNullOrEmpty(orderString))
            {
                sb.Append(" ORDER BY ");
                sb.Append(orderString);
            }

            //OFFSET & LIMIT
            if ((filter.Skip.HasValue || filter.Take.HasValue) && string.IsNullOrEmpty(orderString))
            {
                sb.Append(" ORDER BY ");
                sb.Append(filter.Table.PrimaryKey.ColumnName);
            }

            if (filter.Skip.HasValue || filter.Take.HasValue)
            {
                sb.Append(" OFFSET ");
                sb.Append(filter.Skip ?? 0);
                sb.Append(" ROWS");
            }

            if (filter.Take.HasValue)
            {
                sb.Append(" FETCH NEXT ");
                sb.Append(filter.Take.Value);
                sb.Append(" ROWS ONLY");
            }

            return sb.Append(";").ToString();
        }   
        
        //TODO: To implement
        private bool IsTableExists(string tablename)
        {            
            try
            {               
                SqlCommand cmd = new SqlCommand("SELECT count(*) FROM " + tablename,connection);
                object result = cmd.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }                    
        }     

        #endregion Private Methods
    }
}
