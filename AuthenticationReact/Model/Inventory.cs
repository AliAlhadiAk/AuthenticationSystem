using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationReact.Model
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [ForeignKey("Book")]
        public int BookId { get; set; }

        public int Stock { get; set; }

        public Book Book { get; set; }
    }
}
