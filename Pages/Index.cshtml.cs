using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FACEBOOK_INTEGRATION.Extensions;
using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IClientInterface _clients;

        public IndexModel(ILogger<IndexModel> logger, IClientInterface clients)
        {
            _logger = logger;
            _clients = clients;
        }

        public List<Client> Clients { get; private set; } = new();

        [BindProperty]
        public int SelectedClientId { get; set; }

        public string? CurrentClientLabel { get; private set; }

        public async Task OnGet(string? msg = null, CancellationToken ct = default)
        {
            ViewData["Msg"] = msg;
            Clients = await _clients.GetAllAsync(ct);
            var current = HttpContext.Session.GetClientId();
            if (current is not null)
            {
                var c = Clients.FirstOrDefault(x => x.ClientId == current.Value);
                CurrentClientLabel = c is null ? $"ClientId={current.Value}" : $"{c.Name} (#{c.ClientId})";
            }
        }

        public async Task<IActionResult> OnPostSelectClient(CancellationToken ct)
        {
            Clients = await _clients.GetAllAsync(ct);
            if (!Clients.Any(c => c.ClientId == SelectedClientId))
                return Redirect("/Index?msg=Invalid%20client");

            HttpContext.Session.SetInt32Required(SessionKeys.ClientId, SelectedClientId);
            return Redirect("/Facebook/Integration?msg=Client%20selected");
        }
    }
}
