using Tutorial9.Model.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tutorial9.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<int> AddProductToWarehouse(ProductWarehouseRequestDTO request);
        Task<int> AddProductToWarehouseWithProcedure(ProductWarehouseRequestDTO request);
    }
}