using Dapper;
using DogTracker.Web.Models; // Assuming WeightMeasurement is in this namespace
using Microsoft.Data.SqlClient;

namespace DogTracker.Web.Data;

public class WeightMeasurementRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WeightMeasurementRepository> _logger;

    public WeightMeasurementRepository(IConfiguration configuration, ILogger<WeightMeasurementRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    // Get ALL measurements (Consider GetByDogId as primary)
    public async Task<IEnumerable<WeightMeasurement>> GetAllAsync()
    {
        const string sql = "SELECT Id, DogId, Timestamp, Weight, Unit FROM WeightMeasurements ORDER BY Timestamp DESC";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<WeightMeasurement>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all weight measurements from database.");
            throw;
        }
    }

    // Get measurements FOR A SPECIFIC DOG
    public async Task<IEnumerable<WeightMeasurement>> GetByDogIdAsync(int dogId)
    {
        const string sql = "SELECT Id, DogId, Timestamp, Weight, Unit FROM WeightMeasurements WHERE DogId = @DogId ORDER BY Timestamp DESC";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<WeightMeasurement>(sql, new { DogId = dogId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weight measurements for DogId {DogId} from database.", dogId);
            throw;
        }
    }

    // Get a SINGLE measurement by its own ID
    public async Task<WeightMeasurement?> GetByIdAsync(int id)
    {
        const string sql = "SELECT Id, DogId, Timestamp, Weight, Unit FROM WeightMeasurements WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<WeightMeasurement>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weight measurement with Id {MeasurementId} from database.", id);
            throw;
        }
    }

    // Add a NEW measurement
    public async Task<int> AddAsync(WeightMeasurement measurement)
    {
        if (measurement.DogId <= 0)
        {
            throw new ArgumentException("A valid DogId must be provided.", nameof(measurement.DogId));
        }
        // Basic validation for required fields
        if (measurement.Weight <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(measurement.Weight));
        }
        if (string.IsNullOrWhiteSpace(measurement.Unit))
        {
            throw new ArgumentException("Unit cannot be empty.", nameof(measurement.Unit));
        }
        // Consider setting Timestamp server-side if not provided?
        // if (measurement.Timestamp == default) measurement.Timestamp = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO WeightMeasurements (DogId, Timestamp, Weight, Unit)
            VALUES (@DogId, @Timestamp, @Weight, @Unit);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, measurement);
        }
        catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
        {
            _logger.LogWarning(ex, "Attempted to add weight measurement for non-existent DogId {DogId}", measurement.DogId);
            throw new InvalidOperationException($"Dog with ID {measurement.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding weight measurement {@Measurement} to database.", measurement);
            throw;
        }
    }

    // Update an EXISTING measurement
    public async Task<bool> UpdateAsync(WeightMeasurement measurement)
    {
        if (measurement.Id <= 0)
        {
            _logger.LogWarning("Attempted to update weight measurement with invalid Id {@Measurement}", measurement);
            return false;
        }
        if (measurement.DogId <= 0)
        {
            _logger.LogWarning("Attempted to update weight measurement with invalid DogId {@Measurement}", measurement);
            return false; // Or throw ArgumentException
        }
        if (measurement.Weight <= 0)
        {
            _logger.LogWarning("Attempted to update weight measurement with invalid Weight {@Measurement}", measurement);
            return false; // Or throw ArgumentException
        }
        if (string.IsNullOrWhiteSpace(measurement.Unit))
        {
            _logger.LogWarning("Attempted to update weight measurement with invalid Unit {@Measurement}", measurement);
            return false; // Or throw ArgumentException
        }

        const string sql = @"
            UPDATE WeightMeasurements
            SET DogId = @DogId,
                Timestamp = @Timestamp,
                Weight = @Weight,
                Unit = @Unit
            WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, measurement);
            return affectedRows == 1;
        }
        catch (SqlException ex) when (ex.Number == 547) // Foreign key violation on update (changing DogId?)
        {
            _logger.LogWarning(ex, "Attempted to update weight measurement to non-existent DogId {DogId}", measurement.DogId);
            throw new InvalidOperationException($"Dog with ID {measurement.DogId} not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating weight measurement {@Measurement} in database.", measurement);
            throw;
        }
    }

    // Delete a measurement by its ID
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM WeightMeasurements WHERE Id = @Id";
        try
        {
            using var connection = new SqlConnection(_connectionString);
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting weight measurement with Id {MeasurementId} from database.", id);
            throw;
        }
    }
}