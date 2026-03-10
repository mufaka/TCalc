using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace TCalc.Web.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int StatusCode { get; set; }

        public string ErrorTitle { get; set; } = "Something went wrong";

        public string ErrorMessage { get; set; } = "An unexpected error occurred while processing your request.";

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger) => _logger = logger;

        public void OnGet(int? statusCode = null)
        {
            StatusCode = statusCode ?? HttpContext.Response.StatusCode;
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            switch (StatusCode)
            {
                case 400:
                    ErrorTitle = "Bad Request";
                    ErrorMessage = "The server could not understand the request. Please check your input and try again.";
                    break;
                case 403:
                    ErrorTitle = "Access Denied";
                    ErrorMessage = "You don't have permission to access this resource.";
                    break;
                case 404:
                    ErrorTitle = "Page Not Found";
                    ErrorMessage = "The page you're looking for doesn't exist. It may have been moved or deleted.";
                    break;
                case >= 500:
                    ErrorTitle = "Server Error";
                    ErrorMessage = "Something went wrong on our end. Please try again later.";
                    _logger.LogError("Server error {StatusCode} for request {RequestId}", StatusCode, RequestId);
                    break;
            }
        }
    }

}
