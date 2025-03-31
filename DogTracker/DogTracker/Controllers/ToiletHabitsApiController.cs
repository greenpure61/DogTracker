using DogTracker.Web.Data;
using DogTracker.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DogTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToiletHabitsApiController : ControllerBase
    {
        private readonly ToiletHabitRepository _habitRepository;
        private readonly ILogger<ToiletHabitsApiController> _logger;

        public ToiletHabitsApiController(ToiletHabitRepository habitRepository, ILogger<ToiletHabitsApiController> logger)
        {
            _habitRepository = habitRepository;
            _logger = logger;
        }

        // GET: api/ToiletHabitsApi
        // GET: api/ToiletHabitsApi?dogId=1
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ToiletHabit>>> GetToiletHabits([FromQuery] int? dogId = null)
        {
            _logger.LogInformation("API endpoint called: GET api/ToiletHabitsApi (DogId: {DogId})", dogId?.ToString() ?? "ALL");
            try
            {
                IEnumerable<ToiletHabit> habits;
                if (dogId.HasValue && dogId.Value > 0)
                {
                    habits = await _habitRepository.GetByDogIdAsync(dogId.Value);
                }
                else
                {
                    habits = await _habitRepository.GetAllAsync(); // Or require dogId
                }
                return Ok(habits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting toilet habits via API (DogId: {DogId}).", dogId?.ToString() ?? "ALL");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // GET: api/ToiletHabitsApi/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ToiletHabit>> GetToiletHabit(int id)
        {
            _logger.LogInformation("API endpoint called: GET api/ToiletHabitsApi/{HabitId}", id);
            try
            {
                var habit = await _habitRepository.GetByIdAsync(id);
                if (habit == null)
                {
                    return NotFound();
                }
                return Ok(habit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting toilet habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // POST: api/ToiletHabitsApi
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ToiletHabit>> PostToiletHabit([FromBody] ToiletHabit habit)
        {
            _logger.LogInformation("API endpoint called: POST api/ToiletHabitsApi");
            if (habit == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            habit.Id = 0; // Ensure ID is not set

            try
            {
                var createdHabitId = await _habitRepository.AddAsync(habit);
                habit.Id = createdHabitId;

                return CreatedAtAction(nameof(GetToiletHabit), new { id = createdHabitId }, habit);
            }
            catch (InvalidOperationException ex) // FK violation
            {
                _logger.LogWarning(ex, "Attempted to add toilet habit for non-existent DogId {DogId}", habit.DogId);
                ModelState.AddModelError(nameof(habit.DogId), ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating toilet habit via API.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // PUT: api/ToiletHabitsApi/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutToiletHabit(int id, [FromBody] ToiletHabit habit)
        {
            _logger.LogInformation("API endpoint called: PUT api/ToiletHabitsApi/{HabitId}", id);
            if (id != habit.Id)
            {
                return BadRequest("ID mismatch between URL and request body.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updateSuccessful = await _habitRepository.UpdateAsync(habit);
                if (!updateSuccessful)
                {
                    return NotFound(); // Assume NotFound if update fails
                }
                return NoContent();
            }
            catch (InvalidOperationException ex) // FK violation
            {
                _logger.LogWarning(ex, "Attempted to update toilet habit to non-existent DogId {DogId}", habit.DogId);
                ModelState.AddModelError(nameof(habit.DogId), ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating toilet habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // DELETE: api/ToiletHabitsApi/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteToiletHabit(int id)
        {
            _logger.LogInformation("API endpoint called: DELETE api/ToiletHabitsApi/{HabitId}", id);
            if (id <= 0)
            {
                return BadRequest("Invalid ID supplied.");
            }

            try
            {
                var deleteSuccessful = await _habitRepository.DeleteAsync(id);
                if (!deleteSuccessful)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting toilet habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }
    }
}