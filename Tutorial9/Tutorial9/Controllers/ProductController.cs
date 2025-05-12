using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]  // localhost:XXXX/api/product (ucina "controller") poczatek

public class ProductController : ControllerBase
{
    private readonly IDbService _dbService;

    public ProductController(IDbService dbService)
    {
        _dbService = dbService;
    }

    // 11111111111111
    [HttpGet("products")]  //  localhost:XXXX/api/product/products (zaczyna i dopisuje do poczatku)
//    [HttpGet("api/products")]  //  localhost:XXXX/api/products    (unikatowy wlasny link)
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _dbService.GetProductsAsync();
        return Ok(products);
    }
    // 22222222222222
    [HttpGet("products/{id::int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _dbService.GetProductByIdAsync(id);

        return Ok(product);
    }
}