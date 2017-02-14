using System.Text;

namespace WPO.Helpers
{
    internal static class TransactionExtensions
    {
        internal static void WriteDiagnostics(this string query)
        {
            System.Diagnostics.Debug.WriteLine(query);
        }

        internal static string GetPrimaryKeyQueryString(this PrimaryKey primaryKey)
        {
            StringBuilder sb = new StringBuilder("(");
            sb.Append(primaryKey.ColumnName);
            sb.Append(" = ");
            sb.Append(primaryKey.Value);
            sb.Append(")");
            return sb.ToString();
        }

    }
}
