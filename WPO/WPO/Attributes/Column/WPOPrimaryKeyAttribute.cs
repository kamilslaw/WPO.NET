using System;

namespace WPO.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class WPOPrimaryKeyAttribute : WPOColumnAttribute
    {
        public bool UseSequence { get; private set; }
        public string SequenceName { get; private set; }
        
        public WPOPrimaryKeyAttribute(string columnName = null, bool useSequence = false, string sequenceName = null)
            : base(columnName)
        {
            if (useSequence && string.IsNullOrWhiteSpace(sequenceName))
            {
                throw new ArgumentNullException(nameof(sequenceName), "when sequence is used, its name has to be defined");
            }

            this.UseSequence = useSequence;
            this.SequenceName = sequenceName;
        }
    }
}
