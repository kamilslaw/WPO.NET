using System.Collections.Generic;
using System.Data.SqlClient;

namespace WPO.Connection
{
    internal interface ITransactionStrategy
    {
        void MakeTransaction(IEnumerable<ExecuteCommand> commands, SqlConnection connection);
    }
}
