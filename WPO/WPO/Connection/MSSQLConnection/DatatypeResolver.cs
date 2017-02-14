using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPO.Enums;

namespace WPO.Connection.MSSQLConnection
{
    internal static class DatatypeResolver
    {
        internal static string Resolve(ColumnType colType)
        {
            switch (colType)
            {
                case ColumnType.BOOL:
                    return "BIT";
                case ColumnType.BYTE:
                    return "TINYINT";
                case ColumnType.CHAR:
                    return "CHAR";
                case ColumnType.DATE:
                    return "DATE";
                case ColumnType.DECIMAL:
                    return "DECIMAL";
                case ColumnType.DOUBLE:
                    return "FLOAT";
                case ColumnType.INTEGER:
                    return "INT";
                case ColumnType.LONG:
                    return "BIGINT";
                case ColumnType.MONEY:
                    return "MONEY";
                case ColumnType.SHORT:
                    return "SMALLINT";
                case ColumnType.SINGLE:
                    return "FLOAT";
                case ColumnType.STRING:
                    return "VARCHAR";                
            }
            throw new NotSupportedException();
        }
    }
}
