using Microsoft.Data.SqlClient;
using Tutorial9.Model.DTO;
using Tutorial9.Services.Interfaces;

namespace Tutorial9.Services
{
    public class OrderService : IOrderService
    {
        private readonly string _connectionString;

        public OrderService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrders()
        {
            var orders = new List<OrderDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdOrder, IdProduct, Amount, CreatedAt, FulfilledAt FROM [Order]";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new OrderDTO
                            {
                                IdOrder = reader.GetInt32(0),
                                IdProduct = reader.GetInt32(1),
                                Amount = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                FulfilledAt = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public async Task<OrderDTO> GetOrderById(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdOrder, IdProduct, Amount, CreatedAt, FulfilledAt FROM [Order] WHERE IdOrder = @IdOrder";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdOrder", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new OrderDTO
                            {
                                IdOrder = reader.GetInt32(0),
                                IdProduct = reader.GetInt32(1),
                                Amount = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                FulfilledAt = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4)
                            };
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Order with ID {id} not found");
                        }
                    }
                }
            }
        }

        public async Task<int> CreateOrder(OrderDTO order)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkProductSql = "SELECT COUNT(1) FROM Product WHERE IdProduct = @IdProduct";
                using (SqlCommand checkCommand = new SqlCommand(checkProductSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdProduct", order.IdProduct);
                    int count = (int)await checkCommand.ExecuteScalarAsync();
                    
                    if (count == 0)
                    {
                        throw new ArgumentException($"Product with ID {order.IdProduct} does not exist");
                    }
                }

                string sql = @"
                    INSERT INTO [Order] (IdProduct, Amount, CreatedAt)
                    VALUES (@IdProduct, @Amount, @CreatedAt);
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdProduct", order.IdProduct);
                    command.Parameters.AddWithValue("@Amount", order.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task UpdateOrder(int id, OrderDTO order)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkSql = "SELECT FulfilledAt FROM [Order] WHERE IdOrder = @IdOrder";
                using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdOrder", id);
                    var result = await checkCommand.ExecuteScalarAsync();
                    
                    if (result == null || result == DBNull.Value)
                    {
                        throw new KeyNotFoundException($"Order with ID {id} not found");
                    }
                    
                    if (result != DBNull.Value)
                    {
                        throw new InvalidOperationException("Cannot update an order that has already been fulfilled");
                    }
                }

                string sql = @"
                    UPDATE [Order] 
                    SET IdProduct = @IdProduct, Amount = @Amount, CreatedAt = @CreatedAt
                    WHERE IdOrder = @IdOrder";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdOrder", id);
                    command.Parameters.AddWithValue("@IdProduct", order.IdProduct);
                    command.Parameters.AddWithValue("@Amount", order.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Order with ID {id} not found");
                    }
                }
            }
        }

        public async Task DeleteOrder(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkSql = "SELECT FulfilledAt FROM [Order] WHERE IdOrder = @IdOrder";
                using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdOrder", id);
                    var result = await checkCommand.ExecuteScalarAsync();
                    
                    if (result == null || result == DBNull.Value)
                    {
                        throw new KeyNotFoundException($"Order with ID {id} not found");
                    }
                    
                    if (result != DBNull.Value)
                    {
                        throw new InvalidOperationException("Cannot delete an order that has already been fulfilled");
                    }
                }

                string sql = "DELETE FROM [Order] WHERE IdOrder = @IdOrder";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdOrder", id);
                    
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Order with ID {id} not found");
                    }
                }
            }
        }

        public async Task<IEnumerable<OrderDTO>> GetPendingOrders()
        {
            var orders = new List<OrderDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdOrder, IdProduct, Amount, CreatedAt FROM [Order] WHERE FulfilledAt IS NULL";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new OrderDTO
                            {
                                IdOrder = reader.GetInt32(0),
                                IdProduct = reader.GetInt32(1),
                                Amount = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                FulfilledAt = null
                            });
                        }
                    }
                }
            }

            return orders;
        }
    }
}