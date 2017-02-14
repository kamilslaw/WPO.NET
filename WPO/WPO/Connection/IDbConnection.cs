using System;
using System.Collections.Generic;
using WPO.Schemas;

namespace WPO.Connection
{
    public interface IDbConnection : IDisposable
    {
        /// <summary>
        /// Gets or sets the connection string directly linked with connection
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// Open the connection
        /// </summary>
        void Open();

        /// <summary>
        /// Close the connection
        /// </summary>
        void Close();

        /// <summary>
        /// Execute the reader
        /// </summary>
        /// <param name="filter">query filter</param>
        /// <returns>The list of objects that are given as dictionaries - the key is column name, the value is a row value as string</returns>
        IEnumerable<Dictionary<string,string>> ExecuteReader(QueryFilter filter);

        /// <summary>
        /// Execute INSERT, UPDATE and DELETE commands
        /// </summary>
        /// <param name="commands">The commands list provides by session</param>
        void MakeTransaction(IEnumerable<ExecuteCommand> commands);

        /// <summary>
        /// Gets the sequences values
        /// </summary>
        /// <param name="sequences">The KEY is sequence name, the VALUE is count of needed instances</param>
        /// <returns>Sequences values</returns>
        IEnumerable<ExecuteResult<long>> GetSequences(Dictionary<string, int> sequences);

        void CreateSchema(List<TableSchema> schema);      
    }
}
