using System;
using System.Collections.Generic;

namespace WPO.Connection
{
    public class ExecuteCommand
    {
        public enum CommandType
        {
            Update,
            Insert, 
            Delete
        }

        public ExecuteCommand(CommandType type)
        {
            Type = type;
            ObjectsIdentifiers = new List<PrimaryKey>();
            Objects = new Dictionary<string, Tuple<object, bool>>();
        }

        /// <summary>
        /// The command type
        /// </summary>
        public CommandType Type { get; set; }

        /// <summary>
        /// Connected table name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Using to identify objects while removing or updating, contains one or more keys, depending on the command type
        /// </summary>
        public List<PrimaryKey> ObjectsIdentifiers { get; set; }

        /// <summary>
        /// The Key is column name, and Value is a new row value with information if it is relation, using while updating and inserting
        /// </summary>
        public Dictionary<string, Tuple<object, bool>> Objects { get; set; }
        
        public Tuple<string, int> Sequence { get; set; }
    }
}
