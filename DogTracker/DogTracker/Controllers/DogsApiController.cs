using DogTracker.Models;
using DogTracker.Web.Data;
using DogTracker.Web.Models;
using Microsoft.AspNetCore.Http; // Needed for StatusCodes
using Microsoft.AspNetCore.Mvc;

namespace DogTracker.Web.Controllers // Or change namespace if you put it in an 'Api' subfolder
{
    [Route("api/[controller]")] // Sets the base route to "api/DogsApi"
    [ApiController]            // Enables API-specific features
    public class DogsApiController : ControllerBase // Inherit from ControllerBase for APIs
    {
        private readonly DogRepository _dogRepository;
        private readonly ILogger<DogsApiController> _logger; // Optional logging

        // Inject the repository (and logger) via constructor
        public DogsApiController(DogRepository dogRepository, ILogger<DogsApiController> logger)
        {
            _dogRepository = dogRepository;
            _logger = logger; // Optional
        }

        // GET: api/DogsApi
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)] // Document expected responses
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Dog>>> GetDogs()
        {
            _logger.LogInformation("API endpoint called: GET api/DogsApi"); // Optional logging
            try
            {
                var dogs = await _dogRepository.GetAllAsync();
                return Ok(dogs); // Return 200 OK with the list of dogs
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting all dogs via API.");
                // Return a generic 500 error for internal issues
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // GET: api/DogsApi/5
        [HttpGet("{id}")] // Route parameter for the dog's ID
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Dog>> GetDog(int id)
        {
            _logger.LogInformation("API endpoint called: GET api/DogsApi/{DogId}", id); // Optional logging
            try
            {
                var dog = await _dogRepository.GetByIdAsync(id);

                if (dog == null)
                {
                    _logger.LogWarning("Dog with ID {DogId} not found via API.", id);
                    return NotFound(); // Return 404 Not Found if dog doesn't exist
                }

                return Ok(dog); // Return 200 OK with the specific dog
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting dog {DogId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // POST: api/DogsApi
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Dog>> PostDog([FromBody] Dog dog) // Get dog data from request body
        {
            _logger.LogInformation("API endpoint called: POST api/DogsApi");

            // Basic validation (more can be added using data annotations on the model)
            if (dog == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Important: Ensure the ID is 0 or not set, as the DB generates it.
                // If the client sends an ID, you might ignore it or return BadRequest.
                dog.Id = 0;

                var createdDogId = await _dogRepository.AddAsync(dog);

                if (createdDogId <= 0)
                {
                    _logger.LogError("Failed to create dog in database. AddAsync returned {DogId}", createdDogId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error creating dog resource.");
                }

                // Set the generated ID on the dog object to return it
                dog.Id = createdDogId;

                // Return 201 Created status, add Location header with URL to the new dog,
                // and return the newly created dog object in the body.
                return CreatedAtAction(nameof(GetDog), new { id = createdDogId }, dog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating dog via API.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // --- ADD PUT METHOD ---
        // PUT: api/DogsApi/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutDog(int id, [FromBody] Dog dog)
        {
            _logger.LogInformation("API endpoint called: PUT api/DogsApi/{DogId}", id);

            // Check if the ID in the URL matches the ID in the body
            if (id != dog.Id)
            {
                _logger.LogWarning("Mismatched ID in PUT request. URL ID: {UrlId}, Body ID: {BodyId}", id, dog.Id);
                return BadRequest("ID mismatch between URL and request body.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Optional: Check if the dog exists before trying to update
                // var existingDog = await _dogRepository.GetByIdAsync(id);
                // if (existingDog == null)
                // {
                //     _logger.LogWarning("Dog with ID {DogId} not found for PUT request.", id);
                //     return NotFound();
                // }
                // If you don't check first, UpdateAsync returning false handles non-existence

                var updateSuccessful = await _dogRepository.UpdateAsync(dog);

                if (!updateSuccessful)
                {
                    // This could mean the dog wasn't found, or another update issue occurred.
                    // If you didn't check for existence above, check now or assume NotFound.
                    _logger.LogWarning("Update failed for dog ID {DogId}. It might not exist or another issue occurred.", id);
                    // Check if it exists *now* to differentiate between NotFound and other update errors
                    var exists = await _dogRepository.GetByIdAsync(id) != null;
                    if (!exists)
                    {
                        return NotFound(); // Definitely not found
                    }
                    else
                    {
                        // Update failed for other reason - internal error? Or no actual change?
                        // Depending on requirements, could return 500 or maybe still 204 if no change is OK.
                        // Let's return 500 for unexpected update failure.
                        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during the update process.");
                    }

                }

                // Return 204 No Content signifying successful update without returning the body.
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating dog {DogId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }

        // --- ADD DELETE METHOD ---
        // DELETE: api/DogsApi/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDog(int id)
        {
            _logger.LogInformation("API endpoint called: DELETE api/DogsApi/{DogId}", id);

            if (id <= 0)
            {
                return BadRequest("Invalid ID supplied.");
            }

            try
            {
                // Optional: Check if the dog exists before trying to delete
                // var existingDog = await _dogRepository.GetByIdAsync(id);
                // if (existingDog == null)
                // {
                //     _logger.LogWarning("Dog with ID {DogId} not found for DELETE request.", id);
                //     return NotFound();
                // }
                // If you don't check first, DeleteAsync returning false handles non-existence

                var deleteSuccessful = await _dogRepository.DeleteAsync(id);

                if (!deleteSuccessful)
                {
                    // Dog with the given ID likely did not exist
                    _logger.LogWarning("Delete failed for dog ID {DogId}. It might not exist.", id);
                    return NotFound();
                }

                // Return 204 No Content signifying successful deletion.
                return NoContent();
            }
            catch (Exception ex)
            {
                // Could be a foreign key constraint violation if cascade delete isn't set up
                _logger.LogError(ex, "An error occurred while deleting dog {DogId} via API.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
            }
        }
    }
}