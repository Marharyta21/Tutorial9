
using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model.DTO;
using Tutorial9.Services.Interfaces;

namespace Tutorial9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        /// <summary>
        /// Task 1: Add a product to a warehouse using direct SQL implementation
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseRequestDTO request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required");
                
                if (request.IdProduct <= 0)
                    return BadRequest("IdProduct must be a positive number");
                
                if (request.IdWarehouse <= 0)
                    return BadRequest("IdWarehouse must be a positive number");
                
                if (request.Amount <= 0)
                    return BadRequest("Amount must be greater than 0");
                
                if (string.IsNullOrEmpty(request.CreatedAt))
                    return BadRequest("CreatedAt is required");
                
                if (!DateTime.TryParse(request.CreatedAt, out _))
                    return BadRequest("Invalid CreatedAt date format");
                
                int generatedId = await _warehouseService.AddProductToWarehouse(request);
                
                return Ok(generatedId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Task 2: Add a product to a warehouse using stored procedure
        /// </summary>
        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductToWarehouseWithProcedure([FromBody] ProductWarehouseRequestDTO request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required");
                
                if (request.IdProduct <= 0)
                    return BadRequest("IdProduct must be a positive number");
                
                if (request.IdWarehouse <= 0)
                    return BadRequest("IdWarehouse must be a positive number");
                
                if (request.Amount <= 0)
                    return BadRequest("Amount must be greater than 0");
                
                if (string.IsNullOrEmpty(request.CreatedAt))
                    return BadRequest("CreatedAt is required");
                
                if (!DateTime.TryParse(request.CreatedAt, out _))
                    return BadRequest("Invalid CreatedAt date format");
                
                int generatedId = await _warehouseService.AddProductToWarehouseWithProcedure(request);
                
                return Ok(generatedId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllWarehouses()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehouses();
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWarehouseById(int id)
        {
            try
            {
                var warehouse = await _warehouseService.GetWarehouseById(id);
                return Ok(warehouse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}/products")]
        public async Task<IActionResult> GetWarehouseProducts(int id)
        {
            try
            {
                var products = await _warehouseService.GetWarehouseProducts(id);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> AddWarehouse([FromBody] WarehouseDTO warehouse)
        {
            try
            {
                if (warehouse == null)
                    return BadRequest("Warehouse data is required");

                if (string.IsNullOrEmpty(warehouse.Name))
                    return BadRequest("Warehouse name is required");

                int id = await _warehouseService.AddWarehouse(warehouse);
                return CreatedAtAction(nameof(GetWarehouseById), new { id }, id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] WarehouseDTO warehouse)
        {
            try
            {
                if (warehouse == null)
                    return BadRequest("Warehouse data is required");

                if (string.IsNullOrEmpty(warehouse.Name))
                    return BadRequest("Warehouse name is required");

                await _warehouseService.UpdateWarehouse(id, warehouse);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            try
            {
                await _warehouseService.DeleteWarehouse(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}