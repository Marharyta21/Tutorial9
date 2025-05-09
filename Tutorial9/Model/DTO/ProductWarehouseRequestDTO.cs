namespace Tutorial9.Model.DTO
{
    public class ProductWarehouseRequestDTO
    {
        public int IdProduct { get; set; }
        public int IdWarehouse { get; set; }
        public int Amount { get; set; }
        public string CreatedAt { get; set; }
    }
}