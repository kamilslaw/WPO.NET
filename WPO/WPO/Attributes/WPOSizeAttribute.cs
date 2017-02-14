using System;

namespace WPO.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class WPOSizeAttribute : Attribute
    {
        public int Length { get; private set; }
        public int FractionalPartLength { get; private set; }

        public WPOSizeAttribute(int length, int fractionalPartLength = 0)
        {
            Length = length;
            FractionalPartLength = fractionalPartLength;
        }
    }
}
