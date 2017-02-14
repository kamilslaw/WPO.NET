using System;
using WPO.Enums;

namespace WPO.Attributes
{
    /// <summary>
    /// Using to represent the object or the collection from the connected table - foreign key describes second table connected field name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class WPORelationAttribute : WPOColumnAttribute
    {
        public string ForeignKey { get; private set; }
        public RelationType RelationType { get; private set; }
        
        public WPORelationAttribute(RelationType relationType, string foreignKey, string columnName = null)
            : base(columnName)
        {
            if (string.IsNullOrWhiteSpace(foreignKey))
            {
                throw new ArgumentNullException(nameof(foreignKey));
            }

            ForeignKey = foreignKey;
            RelationType = relationType;
        }
    }
}
