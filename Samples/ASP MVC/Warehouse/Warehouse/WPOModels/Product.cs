using WPO;
using WPO.Attributes;

namespace Warehouse.WPOModels
{
    [WPOTable("dbo.Products")]
    public class Product : WPOBaseObject
    {
        public Product(Session s) : base(s) { }

        [WPOPrimaryKey("ProductID")]
        public int Id { get; set; }

        [WPOColumn("ProductName")]
        public string Name { get; set; }

        [WPOColumn("Price")]
        public decimal? Price { get; set; }

        [WPOColumn("ProductDescription")]
        public string ProductDescription { get; set; }

    }
}