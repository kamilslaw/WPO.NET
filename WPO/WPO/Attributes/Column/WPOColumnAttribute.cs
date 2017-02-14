using System;

namespace WPO.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class WPOColumnAttribute : Attribute
    {
        public string ColumnName { get; private set; }

        //Default column name is lowercase property name
        public WPOColumnAttribute(string columnName = null)
        {
            ColumnName = columnName;
        }
    }
}
