using Tutorial9.Model;
using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public interface IDbService
{
    Task<IEnumerable<Product>> GetProductsAsync();
    Task<Product> GetProductByIdAsync(int id);
    Task<int> AddDataAsync(ProductWarehouseDTO productWarehouseDto);

}