using System.Collections.Generic;
using System.Linq;
using WPO.Enums;
using WPO.Helpers;

namespace WPO
{
    /// <summary>
    /// Using to cache WPO Object database informations
    /// </summary>
    public class WPOTableObject
    {
        public static HashSet<WPOTableObject> ExsistTableObjects = new HashSet<WPOTableObject>();

        public string TableName { get; internal set; }

        public PrimaryKey PrimaryKey { get; internal set; }

        public WPOBaseObject WPOObject { get; internal set; }

        public InheritanceType Inheritance { get; internal set; }

        // Use only when Inheritance is ClassTable
        public WPOTableObject BaseTable { get; internal set; }

        public WPOTableObject(WPOBaseObject obj)
        {
            WPOObject = obj;
            TableName = obj.GetTableName();
            Inheritance = obj.GetInheritanceType();
            PrimaryKey = obj.GetPrimaryKey();

            if (Inheritance == InheritanceType.ClassTable)
            {
                WPOTableObject exsistObj = ExsistTableObjects.FirstOrDefault(e => e.GetType() == obj.GetType().BaseType);
                BaseTable = exsistObj ?? new WPOTableObject(obj.GetType().BaseType.CreateModelObj());
            }
        }
    }
}
