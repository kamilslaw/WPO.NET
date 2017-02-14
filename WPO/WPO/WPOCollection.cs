using System.Collections.Generic;
using System.Linq;

namespace WPO
{
    public class WPOCollection<T> : List<T>
    {
        #region Constructors

        public WPOCollection()
        {
        }

        public WPOCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        public WPOCollection(int capacity) : base(capacity)
        {
        }

        #endregion Constructors

        public WPOCollection<WpoObject> Cast<WpoObject>(WPOCollection<WPOBaseObject> collection)
        {
            return new WPOCollection<WpoObject>(collection.Cast<WpoObject>());
        }
    }
}
