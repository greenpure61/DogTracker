using DogTracker.Web.Data;
using DogTracker.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DogTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeightMeasurementsApiController : ControllerBase
    {
        private readonly WeightMeasurementRepository _measurementRepository;
        private readonly ILogger<WeightMeasurementsApiController> _logger;

        public WeightMeasurementsApiController(WeightMeasurementRepository measurementRepository, ILogger<WeightMeasurementsApiController> logger)
        {
            _measurementRepository = measurementRepository;
            _logger = logger;
        }

        // GET: api/WeightMeasurementsApi
        // GET: api/WeightMeasurementsApi?dogId=1
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<WeightMeasurement>>> GetWeightMeasurements([FromQuery] int? dogId = null)
        {
            _logger.LogInformation("API endpoint called: GET api/WeightMeasurementsApi (DogId: {DogId})", dogId?.ToString() ?? "ALL");
            try
            {
                IEnumerable<WeightMeasurement> measurements;
                if (dogId.HasValue && dogId.Value > 0)
                {
                    measurements = await _measurementRepository.GetByDogIdAsync(dogId.Value);
                }
                else
                {
                    measurements = await _measurementRepository.GetAllAsync(); // Or require dogId
                }
                return Ok(measurements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting weight measurements via API (DogId: {DogId}).", dogId?.ToString() ?? "ALL");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // GET: api/WeightMeasurementsApi/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WeightMeasurement>> GetWeightMeasurement(int id)
        {
            _logger.LogInformation("API endpoint called: GET api/WeightMeasurementsApi/{MeasurementId}", id);
            try
            {
                var measurement = await _measurementRepository.GetByIdAsync(id);
                if (measurement == null)
                {
                    return NotFound();
                }
                return Ok(measurement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting weight measurement {MeasurementId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // POST: api/WeightMeasurementsApi
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WeightMeasurement>> PostWeightMeasurement([FromBody] WeightMeasurement measurement)
        {
            _logger.LogInformation("API endpoint called: POST api/WeightMeasurementsApi");
            if (measurement == null || !ModelState.IsValid)
            {
                // Add manual checks if model annotations aren't sufficient (e.g., Weight > 0)
                if (measurement?.Weight <= 0) ModelState.AddModelError(nameof(measurement.Weight), "Weight must be positive.");
                if (string.IsNullOrWhiteSpace(measurement?.Unit)) ModelState.AddModelError(nameof(measurement.Unit), "Unit is required.");
                if (!ModelState.IsValid) return BadRequest(ModelState);
            }
            measurement.Id = 0; // Ensure ID is not set

            try
            {
                var createdMeasurementId = await _measurementRepository.AddAsync(measurement);
                measurement.Id = createdMeasurementId;

                return CreatedAtAction(nameof(GetWeightMeasurement), new { id = createdMeasurementId }, measurement);
            }
            catch (InvalidOperationException ex) // FK violation
            {
                _logger.LogWarning(ex, "Attempted to add weight measurement for non-existent DogId {DogId}", measurement.DogId);
                ModelState.AddModelError(nameof(measurement.DogId), ex.Message);
                return BadRequest(ModelState);
            }
            catch (ArgumentException ex) // Catch validation errors from repo
            {
                _logger.LogWarning(ex, "Invalid argument provided when adding weight measurement.");
                // Attempt to add the error to the correct field if possible
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating weight measurement via API.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // PUT: api/WeightMeasurementsApi/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutWeightMeasurement(int id, [FromBody] WeightMeasurement measurement)
        {
            _logger.LogInformation("API endpoint called: PUT api/WeightMeasurementsApi/{MeasurementId}", id);
            if (id != measurement.Id)
            {
                return BadRequest("ID mismatch between URL and request body.");
            }
            if (!ModelState.IsValid)
            {
                // Add manual checks if model annotations aren't sufficient
                if (measurement?.Weight <= 0) ModelState.AddModelError(nameof(measurement.Weight), "Weight must be positive.");
                if (string.IsNullOrWhiteSpace(measurement?.Unit)) ModelState.AddModelError(nameof(measurement.Unit), "Unit is required.");
                if (!ModelState.IsValid) return BadRequest(ModelState);
            }

            try
            {
                var updateSuccessful = await _measurementRepository.UpdateAsync(measurement);
                if (!updateSuccessful)
                {
                    // Could be NotFound or validation failure from repo
                    // Check existence if necessary to return 404 vs 400/500
                    var exists = await _measurementRepository.GetByIdAsync(id) != null;
                    if (!exists) return NotFound();
                    else return BadRequest("Update failed, possibly due to invalid data."); // Or 500 if unexpected
                }
                return NoContent();
            }
            catch (InvalidOperationException ex) // FK violation
            {
                _logger.LogWarning(ex, "Attempted to update weight measurement to non-existent DogId {DogId}", measurement.DogId);
                ModelState.AddModelError(nameof(measurement.DogId), ex.Message);
                return BadRequest(ModelState);
            }
            catch (ArgumentException ex) // Catch validation errors from repo
            {
                _logger.LogWarning(ex, "Invalid argument provided when updating weight measurement.");
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating weight measurement {MeasurementId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // DELETE: api/WeightMeasurementsApi/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteWeightMeasurement(int id)
        {
            _logger.LogInformation("API endpoint called: DELETE api/WeightMeasurementsApi/{MeasurementId}", id);
            if (id <= 0)
            {
                return BadRequest("Invalid ID supplied.");
            }

            try
            {
                var deleteSuccessful = await _measurementRepository.DeleteAsync(id);
                if (!deleteSuccessful)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting weight measurement {MeasurementId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }
    }
}