using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Services;
using Tutorial9.Model.CustomersModel;


namespace Tutorial9.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerRentalService _rentalService;

    public CustomersController(CustomerRentalService rentalService)
    {
        _rentalService = rentalService;
    }

    [HttpGet("{id}/rentals")]
    public async Task<IActionResult> GetCustomerRentals(int id)
    {
        try
        {
            var result = await _rentalService.GetCustomerRentalsAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (ApplicationException ex)
        {
            // Można logować: _logger.LogError(ex, "Błąd podczas pobierania wypożyczeń");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
