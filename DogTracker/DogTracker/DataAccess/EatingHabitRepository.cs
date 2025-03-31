using Dapper;
using DogTracker.Web.Models; // Assuming EatingHabit is in this namespace
using Microsoft.Data.SqlClient;

namespace DogTracker.Web.Data;

public class EatingHabitRepository
{
    private readonly string _connectionString;
    private readonly ILogger<EatingHabitRepository> _logger;

    public EatingHabitRepository(IConfiguration configuration, ILogger<EatingHabitRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    // Get ALL eating habits (might be less useful, consider GetByDogId)
    public async Task<IEnumerable<EatingHabit>> GetAllAsync()
    {
        const string sql = "SELECT Id, DogId, Timestamp, FoodType, Amount, Unit, Notes FROM EatingHabits ORDER BY Timestamp DESC"; // Example ordering
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<EatingHabit>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all eating habits from database.");
            throw;
        }
    }

    // Get habits FOR A SPECIFIC DOG
    public async Task<IEnumerable<EatingHabit>> GetByDogIdAsync(int dogId)
    {
        const string sql = "SELECT Id, DogId, Timestamp, FoodType, Amount, Unit, Notes FROM EatingHabits WHERE DogId = @DogId ORDER BY Timestamp DESC";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<EatingHabit>(sql, new { DogId = dogId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching eating habits for DogId {DogId} from database.", dogId);
            throw;
        }
    }

    // Get a SINGLE eating habit by its own ID
    public async Task<EatingHabit?> GetByIdAsync(int id)
    {
        const string sql = "SELECT Id, DogId, Timestamp, FoodType, Amount, Unit, Notes FROM EatingHabits WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EatingHabit>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching eating habit with Id {HabitId} from database.", id);
            throw;
        }
    }

    // Add a NEW eating habit
    public async Task<int> AddAsync(EatingHabit habit)
    {
        // Basic validation: Ensure a valid DogId is provided
        if (habit.DogId <= 0)
        {
            throw new ArgumentException("A valid DogId must be provided.", nameof(habit.DogId));
        }

        // Optional: Default Timestamp if not provided by client?
        // if (habit.Timestamp == default) habit.Timestamp = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO EatingHabits (DogId, Timestamp, FoodType, Amount, Unit, Notes)
            VALUES (@DogId, @Timestamp, @FoodType, @Amount, @Unit, @Notes);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, habit);
        }
        catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
        {
            _logger.LogWarning(ex, "Attempted to add eating habit for non-existent DogId {DogId}", habit.DogId);
            // Rethrow a more specific exception or return a special value? For API, controller will handle.
            throw new InvalidOperationException($"Dog with ID {habit.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding eating habit {@Habit} to database.", habit);
            throw;
        }
    }

    // Update an EXISTING eating habit
    public async Task<bool> UpdateAsync(EatingHabit habit)
    {
        if (habit.Id <= 0)
        {
            _logger.LogWarning("Attempted to update eating habit with invalid Id {@Habit}", habit);
            return false;
        }
        if (habit.DogId <= 0)
        { // Also check DogId on update? Maybe not, depends on requirements.
            _logger.LogWarning("Attempted to update eating habit with invalid DogId {@Habit}", habit);
            return false; // Or throw ArgumentException
        }

        const string sql = @"
            UPDATE EatingHabits
            SET DogId = @DogId,
                Timestamp = @Timestamp,
                FoodType = @FoodType,
                Amount = @Amount,
                Unit = @Unit,
                Notes = @Notes
            WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, habit);
            return affectedRows == 1;
        }
        catch (SqlException ex) when (ex.Number == 547) // Foreign key violation on update (changing DogId?)
        {
            _logger.LogWarning(ex, "Attempted to update eating habit to non-existent DogId {DogId}", habit.DogId);
            throw new InvalidOperationException($"Dog with ID {habit.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating eating habit {@Habit} in database.", habit);
            throw;
        }
    }

    // Delete an eating habit by its ID
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM EatingHabits WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting eating habit with Id {HabitId} from database.", id);
            throw;
        }
    }
}