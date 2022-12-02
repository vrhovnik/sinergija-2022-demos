using IS.Web.Interfaces;
using IS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IS.Web.Pages.Kubek;

public class DashboardPageModel : PageModel
{
    private readonly ILogger<DashboardPageModel> logger;
    private readonly IKubernetesService kubernetesService;

    public DashboardPageModel(ILogger<DashboardPageModel> logger, IKubernetesService kubernetesService)
    {
        this.logger = logger;
        this.kubernetesService = kubernetesService;
    }

    public async Task OnGetAsync()
    {
        logger.LogInformation("Dashoard page loaded at {DateLoaded}, loading cluster information", DateTime.Now);
        BasicInformation = await kubernetesService.GetInfoAsync();
        logger.LogInformation("Loaded {KubernetesName} at {KubernetesAddress}", BasicInformation.Name,
            BasicInformation.Address);
    }

    [BindProperty] public KCInfo BasicInformation { get; set; }
}