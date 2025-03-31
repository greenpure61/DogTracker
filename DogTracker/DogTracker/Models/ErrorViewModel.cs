namespace DogTracker.Models // Make sure this namespace matches your project's Models folder
{
    public class ErrorViewModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the request associated with the error.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Gets a value indicating whether the RequestId should be shown.
        /// This is true if the RequestId is not null or empty.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // You could add other properties here if needed, for example:
        // public string? ErrorMessage { get; set; }
        // public string? ExceptionPath { get; set; }
        // However, be very careful about displaying sensitive exception details
        // in non-development environments. The RequestId is generally safe.
    }
}