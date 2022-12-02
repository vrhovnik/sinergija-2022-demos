using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IS.Web.Pages.Info;

[AllowAnonymous]
public class IndexPageModel : PageModel
{
    private readonly ILogger<IndexPageModel> logger;

    public IndexPageModel(ILogger<IndexPageModel> logger) => this.logger = logger;

    public void OnGet()
    {
        logger.LogInformation("Info page loaded at {DateLoaded}", DateTime.Now);
    }
    
}