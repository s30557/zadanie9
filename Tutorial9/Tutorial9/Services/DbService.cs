using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;
using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("Default");
    }


    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        var products = new List<Product>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqlCommand("SELECT IdProduct, Name, Description, Price FROM Product", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                IdProduct = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Price = reader.GetDecimal(3)
            });
        }

        return products;
    }
    

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var product = new Product();

        
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqlCommand(
            "SELECT IdProduct, Name, Description, Price FROM Product WHERE IdProduct = @id", 
            connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            product.IdProduct = reader.GetInt32(0);
            product.Name = reader.GetString(1);
            product.Description = reader.GetString(2);
            product.Price = reader.GetDecimal(3);
        }

        return product; 
    }
    
    public async Task<int> AddDataAsync(ProductWarehouseDTO productWarehouseDto)
    {
        
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();  // daje mozliwosc przypilnowania zeby wszystko wykonalo sie razem

    try
    {

        //1111111111111111111111111111111111111
        var command1 = new SqlCommand("SELECT * FROM Product WHERE IdProduct = @idp", connection, transaction);
        command1.Parameters.AddWithValue("@idp", productWarehouseDto.IdProduct);
        using var reader1 = await command1.ExecuteReaderAsync();
        await reader1.ReadAsync(); // lepiej zeby bylo przed var price
        if (!reader1.HasRows)
        {
            throw new Exception("Produkt nie znaleziony");
        }

        var price = reader1.GetDecimal(3);
        var command2 = new SqlCommand("SELECT * FROM Warehouse WHERE IdWarehouse = @idw", connection, transaction);
        command2.Parameters.AddWithValue("@idw", productWarehouseDto.IdWarehouse);
        using var reader2 = await command2.ExecuteReaderAsync();
        if (!reader2.HasRows)
        {
            throw new Exception("Magazyn nie znaleziony");
        }

        if (productWarehouseDto.Amount <= 0)
        {
            throw new Exception("Ilosc musi byc wieksza niz 0");
        }

        //22222222222222222222222222222222222
        var command3 = new SqlCommand("SELECT CreatedAt, IdOrder FROM Order WHERE IdProduct = @idp AND Amount = @am",
            connection, transaction);
        command3.Parameters.AddWithValue("@idp", productWarehouseDto.IdProduct);
        command3.Parameters.AddWithValue("@am", productWarehouseDto.Amount);
        using var reader3 = await command3.ExecuteReaderAsync();
        if (!await reader3
                .ReadAsync()) // zadziala tak naprawde podobnie jak HasRows a przy okazji przejdza dane dla var date
        {
            throw new Exception("Nie ma takiego zamowienia");
        }

        var date = reader3.GetDateTime(0);
        if (date >= productWarehouseDto.CreatedAt)
        {
            throw new Exception("Data utworzenia zamowienia jest mniejsza niz data z zadania");
        }

        //33333333333333333333333333333333
        var order = reader3.GetInt32(1);
        var command4 = new SqlCommand("SELECT * FROM Product_Warehouse WHERE IdOrder = @ord", connection, transaction);
        command4.Parameters.AddWithValue("@ord", order);
        using var reader4 = await command4.ExecuteReaderAsync();
        if (reader4.HasRows)
        {
            throw new Exception("To zamówienie zostalo juz zrealizowane");
        }

        //44444444444444444444444444444
        var update = new SqlCommand("UPDATE Order SET FulfilledAt = GETDATE() WHERE IdOrder = @ord", connection,
            transaction);
        update.Parameters.AddWithValue("@ord", order);
        await update.ExecuteNonQueryAsync();
        //5555555555555555555555555555
        var insert =
            new SqlCommand(
                "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) OUTPUT INSERTED.IdProductWarehouse VALUES (@warehouse, @product, @order, @amount, @price, @date)",
                connection, transaction);
        insert.Parameters.AddWithValue("@warehouse", productWarehouseDto.IdWarehouse);
        insert.Parameters.AddWithValue("@product", productWarehouseDto.IdProduct);
        insert.Parameters.AddWithValue("@order", order);
        insert.Parameters.AddWithValue("@amount", productWarehouseDto.Amount);
        insert.Parameters.AddWithValue("@price", productWarehouseDto.Amount * price);
        insert.Parameters.AddWithValue("@date", DateTime.Now);
        await insert.ExecuteNonQueryAsync();

        //6666666666666666666666666666
        int newId = (int)await insert.ExecuteScalarAsync();
        
        
        await transaction.CommitAsync();  // jesli wszystko bylo dobrze do teraz to wszystko bedzie zatwierdzone
        return newId;

    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();  // cofniecia wszystkich zapytan sqlowych w tym isert i update
        throw new Exception("Cos poszlo nie tak");   // przekazanie do kontrolera czyli wejdzie w BadRequest
    }
    }
    
}