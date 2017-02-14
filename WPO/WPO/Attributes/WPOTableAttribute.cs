using System;
using WPO.Enums;

namespace WPO.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WPOTableAttribute : Attribute
    {
        public string TableName { get; set; }

        public InheritanceType Inheritance { get; set; }

        //Default table name is lowercase class name
        public WPOTableAttribute(string tableName = null)
        {
            TableName = tableName;
        }
    }
}
