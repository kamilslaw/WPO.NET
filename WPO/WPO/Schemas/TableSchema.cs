using System.Collections.Generic;
using WPO.Enums;

namespace WPO.Schemas
{
    public class TableSchema
    {
        public string Name { get; set; }
        public List<ColumnSchema> Columns = new List<ColumnSchema>();
        public InheritanceType InType { get; set; }

        public class ColumnSchema
        {
            public bool IsPrimaryKey { get; set; }

            public string Name { get; set; }

            public string SequenceName { get; set; }

            public bool HasForeignKey { get; set; }

            public string ForeignKeyName { get; set; }

            public string ForeignTableName { get; set; }

            public ColumnType ColType { get; set; }

            public bool AllowNull { get; set; }

            public int? Length { get; set; }

            public int? FractionalPartLength { get; set; }
        }       
    }
}
