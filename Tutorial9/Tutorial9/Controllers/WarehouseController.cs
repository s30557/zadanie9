using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Model.DTOs;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]

public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;
    
    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> AddData(ProductWarehouseDTO productWarehouseDto)
    {
        try
        {
            var id = await _dbService.AddDataAsync(productWarehouseDto);
            return Ok(id);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        
    }
    

}