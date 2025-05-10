
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