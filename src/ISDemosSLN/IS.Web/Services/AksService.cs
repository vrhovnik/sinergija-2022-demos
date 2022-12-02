using IS.Web.Interfaces;
using IS.Web.Models;
using IS.Web.Options;
using k8s;
using Microsoft.Extensions.Options;

namespace IS.Web.Services;

public class AksService : IKubernetesService
{
    private readonly IStorageWorker storageWorker;
    private readonly ILogger<AksService> logger;
    private readonly KubekOptions kubekOptions;
        
    public AksService(IOptions<KubekOptions> kubekOptionsValue, 
        IStorageWorker storageWorker, ILogger<AksService> logger)
    {
        this.storageWorker = storageWorker;
        this.logger = logger;
        kubekOptions = kubekOptionsValue.Value;
    }

    public async Task<KCInfo> GetInfoAsync()
    {
        logger.LogInformation("Getting configuration file {ConfigName}", kubekOptions.ConfigFileName);
        var stream = await storageWorker.DownloadFileAsync(kubekOptions.ConfigFileName);
        var config =  KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
        logger.LogInformation("Getting host name from config file");
        return new KCInfo
        {
            Name = config.CurrentContext,
            Address = config.Host
        };
    }

    public async Task<Kubernetes> LoadConfigurationAsync()
    {
        logger.LogInformation("Getting configuration file {ConfigName}", kubekOptions.ConfigFileName);
        var stream = await storageWorker.DownloadFileAsync(kubekOptions.ConfigFileName);
        var config =  KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
        logger.LogInformation("Getting host name from config file");
        return new Kubernetes(config);
    }
}