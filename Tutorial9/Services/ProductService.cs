using Microsoft.Data.SqlClient;
using Tutorial9.Model.DTO;
using Tutorial9.Services.Interfaces;

namespace Tutorial9.Services
{
    public class ProductService : IProductService
    {
        private readonly string _connectionString;

        public ProductService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<ProductDTO>> GetAllProducts()
        {
            var products = new List<ProductDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdProduct, Name, Description, Price FROM Product";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new ProductDTO
                            {
                                IdProduct = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                Price = reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }

            return products;
        }

        public async Task<ProductDTO> GetProductById(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdProduct, Name, Description, Price FROM Product WHERE IdProduct = @IdProduct";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdProduct", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ProductDTO
                            {
                                IdProduct = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                Price = reader.GetDecimal(3)
                            };
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Product with ID {id} not found");
                        }
                    }
                }
            }
        }

        public async Task<int> AddProduct(ProductDTO product)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO Product (Name, Description, Price)
                    VALUES (@Name, @Description, @Price);
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Price", product.Price);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task UpdateProduct(int id, ProductDTO product)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    UPDATE Product 
                    SET Name = @Name, Description = @Description, Price = @Price
                    WHERE IdProduct = @IdProduct";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdProduct", id);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Price", product.Price);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Product with ID {id} not found");
                    }
                }
            }
        }

        public async Task DeleteProduct(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkSql = "SELECT COUNT(1) FROM [Order] WHERE IdProduct = @IdProduct";
                using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdProduct", id);
                    int count = (int)await checkCommand.ExecuteScalarAsync();
                    
                    if (count > 0)
                    {
                        throw new InvalidOperationException("Cannot delete product because it is associated with existing orders");
                    }
                }
                
                string sql = "DELETE FROM Product WHERE IdProduct = @IdProduct";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdProduct", id);
                    
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Product with ID {id} not found");
                    }
                }
            }
        }
    }
}