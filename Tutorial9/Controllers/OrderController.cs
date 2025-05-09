// Controllers/OrderController.cs
using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model.DTO;
using Tutorial9.Services.Interfaces;

namespace Tutorial9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrders();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingOrders()
        {
            try
            {
                var orders = await _orderService.GetPendingOrders();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderById(id);
                return Ok(order);
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

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO order)
        {
            try
            {
                if (order == null)
                    return BadRequest("Order data is required");

                if (order.IdProduct <= 0)
                    return BadRequest("Product ID must be a positive number");

                if (order.Amount <= 0)
                    return BadRequest("Amount must be greater than 0");

                int id = await _orderService.CreateOrder(order);
                return CreatedAtAction(nameof(GetOrderById), new { id }, id);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDTO order)
        {
            try
            {
                if (order == null)
                    return BadRequest("Order data is required");

                if (order.IdProduct <= 0)
                    return BadRequest("Product ID must be a positive number");

                if (order.Amount <= 0)
                    return BadRequest("Amount must be greater than 0");

                await _orderService.UpdateOrder(id, order);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                await _orderService.DeleteOrder(id);
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