using System.ComponentModel.DataAnnotations;

namespace Warehouse.Models
{
    public class ProductEditModel
    {
        [Required(ErrorMessage = "Product id is required!")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required!")]
        [MaxLength(25, ErrorMessage = "Max length is 25")]
        public string Name { get; set; }

        public decimal? Price { get; set; }

        public string Description { get; set; }
    }
}