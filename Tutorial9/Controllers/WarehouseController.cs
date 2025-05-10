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

        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
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

        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductToWarehouseWithProcedure([FromBody] ProductWarehouseRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
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
    }
}