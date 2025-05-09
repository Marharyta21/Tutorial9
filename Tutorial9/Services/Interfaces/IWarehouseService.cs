using Tutorial9.Model.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tutorial9.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<int> AddProductToWarehouse(ProductWarehouseRequestDTO request);
        Task<int> AddProductToWarehouseWithProcedure(ProductWarehouseRequestDTO request);
        Task<IEnumerable<WarehouseDTO>> GetAllWarehouses();
        Task<WarehouseDTO> GetWarehouseById(int id);
        Task<int> AddWarehouse(WarehouseDTO warehouse);
        Task UpdateWarehouse(int id, WarehouseDTO warehouse);
        Task DeleteWarehouse(int id);
        Task<IEnumerable<ProductWarehouseDTO>> GetWarehouseProducts(int warehouseId);
    }
}