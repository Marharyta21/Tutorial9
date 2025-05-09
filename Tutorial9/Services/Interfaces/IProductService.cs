using Tutorial9.Model.DTO;

namespace Tutorial9.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDTO>> GetAllProducts();
        Task<ProductDTO> GetProductById(int id);
        Task<int> AddProduct(ProductDTO product);
        Task UpdateProduct(int id, ProductDTO product);
        Task DeleteProduct(int id);
    }
}