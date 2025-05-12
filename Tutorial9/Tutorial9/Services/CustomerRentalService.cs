using Microsoft.Data.SqlClient;

namespace Tutorial9.Services;

public class CustomerRentalService
{
    private readonly string _connectionString;

    public CustomerRentalService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<object> GetCustomerRentalsAsync(int customerId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Sprawdzenie klienta
            var customerCmd = new SqlCommand("SELECT first_name, last_name FROM Customer WHERE customer_id = @id", connection);
            customerCmd.Parameters.AddWithValue("@id", customerId);

            using var reader = await customerCmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            var firstName = reader["first_name"].ToString();
            var lastName = reader["last_name"].ToString();
            await reader.CloseAsync();

            // Pobierz wypożyczenia i filmy
            var rentalCmd = new SqlCommand(@"
                SELECT r.rental_id, r.rental_date, r.return_date, s.name AS status,
                       m.title, ri.price_at_rental
                FROM Rental r
                JOIN Status s ON r.status_id = s.status_id
                JOIN Rental_Item ri ON r.rental_id = ri.rental_id
                JOIN Movie m ON ri.movie_id = m.movie_id
                WHERE r.customer_id = @id
                ORDER BY r.rental_id", connection);
            rentalCmd.Parameters.AddWithValue("@id", customerId);

            var rentals = new List<dynamic>();
            var rentalDict = new Dictionary<int, dynamic>();

            using var rentalReader = await rentalCmd.ExecuteReaderAsync();
            while (await rentalReader.ReadAsync())
            {
                int rentalId = (int)rentalReader["rental_id"];

                if (!rentalDict.ContainsKey(rentalId))
                {
                    var rental = new
                    {
                        id = rentalId,
                        rentalDate = (DateTime)rentalReader["rental_date"],
                        returnDate = rentalReader["return_date"] == DBNull.Value ? null : (DateTime?)rentalReader["return_date"],
                        status = rentalReader["status"].ToString(),
                        movies = new List<dynamic>()
                    };

                    rentalDict[rentalId] = rental;
                    rentals.Add(rental);
                }

                ((List<dynamic>)rentalDict[rentalId].movies).Add(new
                {
                    title = rentalReader["title"].ToString(),
                    priceAtRental = Convert.ToDecimal(rentalReader["price_at_rental"])
                });
            }

            return new
            {
                firstName,
                lastName,
                rentals
            };
        }
        catch (Exception ex)
        {
            // W logice produkcyjnej: logowanie wyjątku
            throw new ApplicationException("Błąd podczas pobierania danych klienta.", ex);
        }
    }
    
    public async Task<bool> AddRentalAsync(int customerId, dynamic rentalData)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        // Sprawdź czy klient istnieje
        var checkCustomerCmd = new SqlCommand("SELECT COUNT(*) FROM Customer WHERE customer_id = @id", connection, transaction);
        checkCustomerCmd.Parameters.AddWithValue("@id", customerId);

        var customerExists = (int)await checkCustomerCmd.ExecuteScalarAsync() > 0;
        if (!customerExists)
        {
            transaction.Rollback();
            return false;
        }

        // Sprawdź czy wszystkie filmy istnieją
        foreach (var movie in rentalData.movies)
        {
            var checkMovieCmd = new SqlCommand("SELECT COUNT(*) FROM Movie WHERE title = @title", connection, transaction);
            checkMovieCmd.Parameters.AddWithValue("@title", (string)movie.title);

            var movieExists = (int)await checkMovieCmd.ExecuteScalarAsync() > 0;
            if (!movieExists)
            {
                transaction.Rollback();
                return false;
            }
        }

        // Dodaj wpis do Rental
        var insertRentalCmd = new SqlCommand(@"
            INSERT INTO Rental (rental_id, rental_date, customer_id, status_id)
            VALUES (@id, @date, @customer_id, @status_id)", connection, transaction);
        insertRentalCmd.Parameters.AddWithValue("@id", (int)rentalData.id);
        insertRentalCmd.Parameters.AddWithValue("@date", (DateTime)rentalData.rentalDate);
        insertRentalCmd.Parameters.AddWithValue("@customer_id", customerId);
        insertRentalCmd.Parameters.AddWithValue("@status_id", 1); // zakładamy "1 = Rented"

        await insertRentalCmd.ExecuteNonQueryAsync();

        // Dodaj wpisy do Rental_Item
        foreach (var movie in rentalData.movies)
        {
            // Pobierz movie_id
            var getMovieIdCmd = new SqlCommand("SELECT movie_id FROM Movie WHERE title = @title", connection, transaction);
            getMovieIdCmd.Parameters.AddWithValue("@title", (string)movie.title);
            var movieId = (int)(await getMovieIdCmd.ExecuteScalarAsync());

            var insertItemCmd = new SqlCommand(@"
                INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental)
                VALUES (@rental_id, @movie_id, @price)", connection, transaction);
            insertItemCmd.Parameters.AddWithValue("@rental_id", (int)rentalData.id);
            insertItemCmd.Parameters.AddWithValue("@movie_id", movieId);
            insertItemCmd.Parameters.AddWithValue("@price", (decimal)movie.rentalPrice);

            await insertItemCmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return true;
    }
    catch (Exception ex)
    {
            // Loguj jeśli trzeba
        return false;
    }
}

    
}
