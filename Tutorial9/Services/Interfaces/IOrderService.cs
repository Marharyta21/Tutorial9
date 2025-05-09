using Tutorial9.Model.DTO;

namespace Tutorial9.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDTO>> GetAllOrders();
        Task<OrderDTO> GetOrderById(int id);
        Task<int> CreateOrder(OrderDTO order);
        Task UpdateOrder(int id, OrderDTO order);
        Task DeleteOrder(int id);
        Task<IEnumerable<OrderDTO>> GetPendingOrders();
    }
}