using DogTracker.Web.Data;
using DogTracker.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DogTracker.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DogTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EatingHabitsApiController : ControllerBase
    {
        private readonly EatingHabitRepository _habitRepository;
        private readonly ILogger<EatingHabitsApiController> _logger;
        private readonly IHubContext<DogUpdateHub> _hubContext;

        public EatingHabitsApiController(EatingHabitRepository habitRepository, ILogger<EatingHabitsApiController> logger, IHubContext<DogUpdateHub> hubContext)
        {
            _habitRepository = habitRepository;
            _logger = logger;
            _hubContext = hubContext;
        }

        // GET: api/EatingHabitsApi
        // GET: api/EatingHabitsApi?dogId=1  <- Filter by dog
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EatingHabit>>> GetEatingHabits([FromQuery] int? dogId = null) // Optional query param
        {
            _logger.LogInformation("API endpoint called: GET api/EatingHabitsApi (DogId: {DogId})", dogId?.ToString() ?? "ALL");
            try
            {
                IEnumerable<EatingHabit> habits;
                if (dogId.HasValue && dogId.Value > 0)
                {
                    habits = await _habitRepository.GetByDogIdAsync(dogId.Value);
                }
                else
                {
                    // Consider if you really want to allow fetching ALL habits without filtering
                    habits = await _habitRepository.GetAllAsync();
                    // Or return BadRequest("dogId is required"); if you don't want GetAll
                }
                return Ok(habits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting eating habits via API (DogId: {DogId}).", dogId?.ToString() ?? "ALL");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // GET: api/EatingHabitsApi/5  (Get specific habit by its own ID)
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EatingHabit>> GetEatingHabit(int id)
        {
            _logger.LogInformation("API endpoint called: GET api/EatingHabitsApi/{HabitId}", id);
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
                _logger.LogError(ex, "An error occurred while getting eating habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // POST: api/EatingHabitsApi
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EatingHabit>> PostEatingHabit([FromBody] EatingHabit habit)
        {
            _logger.LogInformation("API endpoint called: POST api/EatingHabitsApi");
            if (habit == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Ensure ID is not set by client
            habit.Id = 0;

            try
            {
                var createdHabitId = await _habitRepository.AddAsync(habit);
                habit.Id = createdHabitId; // Assign the generated ID back

                // --- SEND NOTIFICATION ---
                string groupName = $"dog-{habit.DogId}";
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveUpdateNotification", habit.DogId, "EatingHabit");
                _logger.LogInformation("Sent SignalR notification to group {GroupName} for new EatingHabit", groupName);
                // ------------------------

                return CreatedAtAction(nameof(GetEatingHabit), new { id = createdHabitId }, habit);
            }
            catch (InvalidOperationException ex) // Catch specific FK violation from repository
            {
                _logger.LogWarning(ex, "Attempted to add eating habit for non-existent DogId {DogId}", habit.DogId);
                // Return BadRequest as the client sent an invalid DogId
                ModelState.AddModelError(nameof(habit.DogId), ex.Message); // Add model error
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating eating habit via API.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // PUT: api/EatingHabitsApi/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutEatingHabit(int id, [FromBody] EatingHabit habit)
        {
            _logger.LogInformation("API endpoint called: PUT api/EatingHabitsApi/{HabitId}", id);
            if (id != habit.Id)
            {
                _logger.LogWarning("Mismatched ID in PUT request. URL ID: {UrlId}, Body ID: {BodyId}", id, habit.Id);
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
                    // Could be NotFound or other issue. Check existence if critical.
                    _logger.LogWarning("Update failed for eating habit ID {HabitId}. It might not exist.", id);
                    // We'll assume NotFound for simplicity if update returns false
                    return NotFound();
                }

                // --- SEND NOTIFICATION ---
                string groupName = $"dog-{habit.DogId}";
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveUpdateNotification", habit.DogId, "EatingHabit");
                _logger.LogInformation("Sent SignalR notification to group {GroupName} for updated EatingHabit", groupName);
                // ------------------------


                return NoContent();
            }
            catch (InvalidOperationException ex) // Catch specific FK violation from repository
            {
                _logger.LogWarning(ex, "Attempted to update eating habit to non-existent DogId {DogId}", habit.DogId);
                ModelState.AddModelError(nameof(habit.DogId), ex.Message);
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating eating habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // DELETE: api/EatingHabitsApi/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEatingHabit(int id)
        {
            _logger.LogInformation("API endpoint called: DELETE api/EatingHabitsApi/{HabitId}", id);
            if (id <= 0)
            {
                return BadRequest("Invalid ID supplied.");
            }

            int dogIdToNotify = 0; // Variable to store the dogId

            try
            {
                // Get the habit first to find its DogId
                var habitToDelete = await _habitRepository.GetByIdAsync(id);
                if (habitToDelete == null)
                {
                    return NotFound();
                }
                dogIdToNotify = habitToDelete.DogId; // Store the DogId

                // Now perform the delete
                var deleteSuccessful = await _habitRepository.DeleteAsync(id);
                if (!deleteSuccessful)
                {
                    // Should ideally not happen if GetByIdAsync succeeded, but handle defensively
                    _logger.LogWarning("Delete failed for eating habit ID {HabitId} after confirming existence.", id);
                    return NotFound(); // Or InternalServerError
                }

                // --- SEND NOTIFICATION ---
                if (dogIdToNotify > 0)
                {
                    string groupName = $"dog-{dogIdToNotify}";
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveUpdateNotification", dogIdToNotify, "EatingHabit");
                    _logger.LogInformation("Sent SignalR notification to group {GroupName} for deleted EatingHabit", groupName);
                }
                // ------------------------

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting eating habit {HabitId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }
    }
}