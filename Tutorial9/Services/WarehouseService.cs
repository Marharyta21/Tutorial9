using Microsoft.Data.SqlClient;
using System.Data;
using Tutorial9.Model.DTO;
using Tutorial9.Services.Interfaces;

namespace Tutorial9.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly string _connectionString;

        public WarehouseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        public async Task<int> AddProductToWarehouse(ProductWarehouseRequestDTO request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than 0");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        bool productExists = await CheckIfProductExists(request.IdProduct, connection, transaction);
                        if (!productExists)
                            throw new ArgumentException($"Product with ID {request.IdProduct} does not exist");
                        
                        bool warehouseExists = await CheckIfWarehouseExists(request.IdWarehouse, connection, transaction);
                        if (!warehouseExists)
                            throw new ArgumentException($"Warehouse with ID {request.IdWarehouse} does not exist");
                        
                        int? orderId = await FindMatchingOrder(request, connection, transaction);
                        if (!orderId.HasValue)
                            throw new ArgumentException("No matching order found");
                        
                        bool orderCompleted = await IsOrderCompleted(orderId.Value, connection, transaction);
                        if (orderCompleted)
                            throw new ArgumentException($"Order with ID {orderId.Value} has already been completed");
                        
                        decimal productPrice = await GetProductPrice(request.IdProduct, connection, transaction);
                        
                        await UpdateOrderFulfillment(orderId.Value, connection, transaction);
                        
                        int generatedId = await InsertProductWarehouse(
                            request.IdProduct, 
                            request.IdWarehouse, 
                            orderId.Value, 
                            request.Amount, 
                            productPrice * request.Amount, 
                            connection, 
                            transaction);
                        
                        transaction.Commit();
                        
                        return generatedId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public async Task<int> AddProductToWarehouseWithProcedure(ProductWarehouseRequestDTO request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than 0");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
        
                using (SqlCommand command = new SqlCommand("AddProductToWarehouse", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
            
                    command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
                    command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
                    command.Parameters.AddWithValue("@Amount", request.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Parse(request.CreatedAt));
                    
                    var result = await command.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                        throw new ArgumentException("Stored procedure execution failed: No ID returned");
            
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<IEnumerable<WarehouseDTO>> GetAllWarehouses()
        {
            var warehouses = new List<WarehouseDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdWarehouse, Name, Address FROM Warehouse";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            warehouses.Add(new WarehouseDTO
                            {
                                IdWarehouse = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Address = !reader.IsDBNull(2) ? reader.GetString(2) : null
                            });
                        }
                    }
                }
            }

            return warehouses;
        }

        public async Task<WarehouseDTO> GetWarehouseById(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT IdWarehouse, Name, Address FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdWarehouse", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new WarehouseDTO
                            {
                                IdWarehouse = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Address = !reader.IsDBNull(2) ? reader.GetString(2) : null
                            };
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Warehouse with ID {id} not found");
                        }
                    }
                }
            }
        }

        public async Task<int> AddWarehouse(WarehouseDTO warehouse)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO Warehouse (Name, Address)
                    VALUES (@Name, @Address);
                    SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", warehouse.Name);
                    command.Parameters.AddWithValue("@Address", warehouse.Address ?? (object)DBNull.Value);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task UpdateWarehouse(int id, WarehouseDTO warehouse)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    UPDATE Warehouse 
                    SET Name = @Name, Address = @Address
                    WHERE IdWarehouse = @IdWarehouse";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdWarehouse", id);
                    command.Parameters.AddWithValue("@Name", warehouse.Name);
                    command.Parameters.AddWithValue("@Address", warehouse.Address ?? (object)DBNull.Value);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Warehouse with ID {id} not found");
                    }
                }
            }
        }

        public async Task DeleteWarehouse(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                string checkSql = "SELECT COUNT(1) FROM Product_Warehouse WHERE IdWarehouse = @IdWarehouse";
                using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdWarehouse", id);
                    int count = (int)await checkCommand.ExecuteScalarAsync();
                    
                    if (count > 0)
                    {
                        throw new InvalidOperationException("Cannot delete warehouse because it contains products");
                    }
                }

                string sql = "DELETE FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdWarehouse", id);
                    
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new KeyNotFoundException($"Warehouse with ID {id} not found");
                    }
                }
            }
        }

        public async Task<IEnumerable<ProductWarehouseDTO>> GetWarehouseProducts(int warehouseId)
        {
            var products = new List<ProductWarehouseDTO>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT pw.IdProductWarehouse, pw.IdWarehouse, pw.IdProduct, pw.IdOrder, 
                           pw.Amount, pw.Price, pw.CreatedAt
                    FROM Product_Warehouse pw
                    WHERE pw.IdWarehouse = @IdWarehouse";
                    
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IdWarehouse", warehouseId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new ProductWarehouseDTO
                            {
                                IdProductWarehouse = reader.GetInt32(0),
                                IdWarehouse = reader.GetInt32(1),
                                IdProduct = reader.GetInt32(2),
                                IdOrder = reader.GetInt32(3),
                                Amount = reader.GetInt32(4),
                                Price = reader.GetDecimal(5),
                                CreatedAt = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }

            return products;
        }
        
        private async Task<bool> CheckIfProductExists(int productId, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = "SELECT COUNT(1) FROM Product WHERE IdProduct = @IdProduct";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdProduct", productId);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task<bool> CheckIfWarehouseExists(int warehouseId, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = "SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdWarehouse", warehouseId);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task<int?> FindMatchingOrder(ProductWarehouseRequestDTO request, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = @"
                SELECT TOP 1 IdOrder
                FROM [Order]
                WHERE IdProduct = @IdProduct 
                AND Amount = @Amount
                AND CreatedAt < @CreatedAt
                AND (FulfilledAt IS NULL)";

            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
                command.Parameters.AddWithValue("@Amount", request.Amount);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Parse(request.CreatedAt));
                
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return null;
                
                return (int)result;
            }
        }

        private async Task<bool> IsOrderCompleted(int orderId, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = "SELECT COUNT(1) FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdOrder", orderId);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        private async Task<decimal> GetProductPrice(int productId, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdProduct", productId);
                return (decimal)await command.ExecuteScalarAsync();
            }
        }

        private async Task UpdateOrderFulfillment(int orderId, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
                command.Parameters.AddWithValue("@IdOrder", orderId);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<int> InsertProductWarehouse(int productId, int warehouseId, int orderId, int amount, decimal price, SqlConnection connection, SqlTransaction transaction)
        {
            string sql = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                SELECT SCOPE_IDENTITY();";
            
            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdWarehouse", warehouseId);
                command.Parameters.AddWithValue("@IdProduct", productId);
                command.Parameters.AddWithValue("@IdOrder", orderId);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
    }
}