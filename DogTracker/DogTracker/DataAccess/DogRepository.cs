// In Data/DogRepository.cs (Example using Dapper)
using Dapper;
using DogTracker.Models;
using DogTracker.Web.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DogTracker.Web.Data;

public class DogRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DogRepository> _logger;

    // Constructor receives connection string via DI
    public DogRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // Example method to get all dogs
    public async Task<IEnumerable<Dog>> GetAllAsync()
    {
        const string sql = "SELECT Id, Name, Breed, DateOfBirth FROM Dogs";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<Dog>(sql);
    }

    // Example method to add a dog
    public async Task<int> AddAsync(Dog dog)
    {
        const string sql = @"
            INSERT INTO Dogs (Name, Breed, DateOfBirth)
            VALUES (@Name, @Breed, @DateOfBirth);
            SELECT CAST(SCOPE_IDENTITY() as int)"; // Get the ID of the inserted row
        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, dog);
    }

    // Add other methods for GetById, Update, Delete, etc.
    // Add similar repositories for EatingHabit, ToiletHabit, WeightMeasurement

    public async Task<Dog?> GetByIdAsync(int id) // Return nullable Dog (Dog?)
    {
        const string sql = "SELECT Id, Name, Breed, DateOfBirth FROM Dogs WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            // Use QueryFirstOrDefaultAsync as we expect 0 or 1 result
            return await connection.QueryFirstOrDefaultAsync<Dog>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching dog with Id {DogId} from database.", id); // Optional logging
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Dog dog)
    {
        // Ensure Id is set for the WHERE clause
        if (dog.Id <= 0)
        {
            _logger?.LogWarning("Attempted to update dog with invalid Id {@Dog}", dog);
            return false; // Or throw an argument exception
        }

        const string sql = @"
        UPDATE Dogs
        SET Name = @Name,
            Breed = @Breed,
            DateOfBirth = @DateOfBirth
        WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            // ExecuteAsync returns the number of rows affected
            var affectedRows = await connection.ExecuteAsync(sql, dog);
            // Return true if exactly one row was updated
            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating dog {@Dog} in database.", dog);
            throw;
        }
    }

    // --- ADD THIS METHOD ---
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Dogs WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            // ExecuteAsync returns the number of rows affected
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            // Return true if exactly one row was deleted
            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            // Consider potential foreign key constraint issues if cascading delete isn't set
            _logger?.LogError(ex, "Error deleting dog with Id {DogId} from database.", id);
            throw;
        }
    }


}