using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FACEBOOK_INTEGRATION.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int? StatusCode { get; private set; }
        public string? ErrorMessage { get; private set; }

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? code = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = code;

            var exFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exFeature?.Error is not null)
            {
                ErrorMessage = exFeature.Error.Message;
                _logger.LogError(exFeature.Error, "Unhandled exception at path {Path}", exFeature.Path);
            }
        }
    }

}
