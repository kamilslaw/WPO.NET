using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPO.Enums;
using System.Reflection;

namespace WPO.Helpers
{
    public static class Converter
    {
        internal static ColumnType TypeToEnum(Type proptype)
        {
            string propName = proptype.Name;
            switch (propName)
            {
                case "Int32":
                    return ColumnType.INTEGER;
                case "Int16":
                    return ColumnType.SHORT;
                case "Int64":
                    return ColumnType.LONG;
                case "Double":
                    return ColumnType.DOUBLE;
                case "Byte":
                    return ColumnType.BYTE;
                case "String":
                    return ColumnType.STRING;
                case "Char":
                    return ColumnType.CHAR;
                case "Boolean":
                    return ColumnType.BOOL;
                case "Decimal":
                    return ColumnType.DECIMAL;
                case "Single":
                    return ColumnType.SINGLE;
                case "DateTime":
                    return ColumnType.DATE;
                case "Money":
                    return ColumnType.MONEY;
                default:
                    throw new NotSupportedException("property name: '" + proptype.Name + "'");
            }
        }

        internal static string EnumToDefaultString(ColumnType type)
        {
            switch(type)
            {
                case ColumnType.DOUBLE:
                case ColumnType.SINGLE:
                case ColumnType.DECIMAL:
                case ColumnType.MONEY:
                    return "(7, 2)";
                case ColumnType.STRING:
                    return "(40)";
                default:
                    return "";
            }
        }
    }
}
