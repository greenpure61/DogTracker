using Dapper;
using DogTracker.Web.Models; // Assuming ToiletHabit is in this namespace
using Microsoft.Data.SqlClient;

namespace DogTracker.Web.Data;

public class ToiletHabitRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ToiletHabitRepository> _logger;

    public ToiletHabitRepository(IConfiguration configuration, ILogger<ToiletHabitRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    // Get ALL toilet habits (Consider GetByDogId as primary)
    public async Task<IEnumerable<ToiletHabit>> GetAllAsync()
    {
        const string sql = "SELECT Id, DogId, Timestamp, Type, Location, Notes FROM ToiletHabits ORDER BY Timestamp DESC";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ToiletHabit>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all toilet habits from database.");
            throw;
        }
    }

    // Get habits FOR A SPECIFIC DOG
    public async Task<IEnumerable<ToiletHabit>> GetByDogIdAsync(int dogId)
    {
        const string sql = "SELECT Id, DogId, Timestamp, Type, Location, Notes FROM ToiletHabits WHERE DogId = @DogId ORDER BY Timestamp DESC";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ToiletHabit>(sql, new { DogId = dogId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching toilet habits for DogId {DogId} from database.", dogId);
            throw;
        }
    }

    // Get a SINGLE toilet habit by its own ID
    public async Task<ToiletHabit?> GetByIdAsync(int id)
    {
        const string sql = "SELECT Id, DogId, Timestamp, Type, Location, Notes FROM ToiletHabits WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ToiletHabit>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching toilet habit with Id {HabitId} from database.", id);
            throw;
        }
    }

    // Add a NEW toilet habit
    public async Task<int> AddAsync(ToiletHabit habit)
    {
        if (habit.DogId <= 0)
        {
            throw new ArgumentException("A valid DogId must be provided.", nameof(habit.DogId));
        }

        // Consider setting Timestamp server-side if not provided? Depends on requirements.
        // if (habit.Timestamp == default) habit.Timestamp = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO ToiletHabits (DogId, Timestamp, Type, Location, Notes)
            VALUES (@DogId, @Timestamp, @Type, @Location, @Notes);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, habit);
        }
        catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
        {
            _logger.LogWarning(ex, "Attempted to add toilet habit for non-existent DogId {DogId}", habit.DogId);
            throw new InvalidOperationException($"Dog with ID {habit.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding toilet habit {@Habit} to database.", habit);
            throw;
        }
    }

    // Update an EXISTING toilet habit
    public async Task<bool> UpdateAsync(ToiletHabit habit)
    {
        if (habit.Id <= 0)
        {
            _logger.LogWarning("Attempted to update toilet habit with invalid Id {@Habit}", habit);
            return false;
        }
        if (habit.DogId <= 0)
        {
            _logger.LogWarning("Attempted to update toilet habit with invalid DogId {@Habit}", habit);
            return false; // Or throw ArgumentException
        }

        const string sql = @"
            UPDATE ToiletHabits
            SET DogId = @DogId,
                Timestamp = @Timestamp,
                Type = @Type,
                Location = @Location,
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
            _logger.LogWarning(ex, "Attempted to update toilet habit to non-existent DogId {DogId}", habit.DogId);
            throw new InvalidOperationException($"Dog with ID {habit.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating toilet habit {@Habit} in database.", habit);
            throw;
        }
    }

    // Delete a toilet habit by its ID
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM ToiletHabits WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting toilet habit with Id {HabitId} from database.", id);
            throw;
        }
    }
}