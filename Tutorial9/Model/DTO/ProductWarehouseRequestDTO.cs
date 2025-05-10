using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model.DTO
{
    public class ProductWarehouseRequestDTO
    {
        [Required(ErrorMessage = "IdProduct is required")]
        [Range(1, int.MaxValue, ErrorMessage = "IdProduct must be greater than 0")]
        public int IdProduct { get; set; }
        
        [Required(ErrorMessage = "IdWarehouse is required")]
        [Range(1, int.MaxValue, ErrorMessage = "IdWarehouse must be greater than 0")]
        public int IdWarehouse { get; set; }
        
        [Required(ErrorMessage = "Amount is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public int Amount { get; set; }
        
        [Required(ErrorMessage = "CreatedAt is required")]
        public string CreatedAt { get; set; }
    }
}