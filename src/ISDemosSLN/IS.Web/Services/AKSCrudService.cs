using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using IS.Web.Interfaces;
using IS.Web.Options;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace IS.Web.Services;

public class AKSCrudService : IKubernetesCrud
{
    private readonly IKubernetesService client;
    private readonly ILogger<AKSCrudService> logger;
    private readonly ArmClient azure;
    private readonly AzureAdOptions azureAdOptions;

    public AKSCrudService(IKubernetesService client, IOptions<AzureAdOptions> azureAdOptionsValue,
        ILogger<AKSCrudService> logger)
    {
        this.client = client;
        this.logger = logger;
        azureAdOptions = azureAdOptionsValue.Value;
        azure = new ArmClient(new ClientSecretCredential(azureAdOptions.TenantId, azureAdOptions.ClientId,
            azureAdOptions.ClientSecret));
    }

    public async Task<bool> CreateNamespaceAsync(string name)
    {
        logger.LogInformation("Getting cluster information to authenticate at {DateLoaded}", DateTime.Now);
        var kubernetes = await client.LoadConfigurationAsync();
        logger.LogInformation("Creating namespace {NamespaceName}", name);

        var ns = new V1Namespace
        {
            Metadata = new V1ObjectMeta { Name = name }
        };

        try
        {
            kubernetes.CreateNamespace(ns);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }

        return false;
    }

    public async Task<bool> CreatePodAsync(string namespaceName, string podname, string image)
    {
        logger.LogInformation("Getting cluster information - client - CreateNamespaceAsync");
        var kubernetes = await client.LoadConfigurationAsync();
        logger.LogInformation("Creating namespace {NamespaceName}", namespaceName);

        var pod = new V1Pod
        {
            Metadata = new V1ObjectMeta
            {
                Name = podname, Labels = new Dictionary<string, string>
                {
                    { "app", podname }
                }
            },
            Spec = new V1PodSpec
            {
                Containers = new List<V1Container>
                {
                    new()
                    {
                        Image = image,
                        Name = $"image-{podname}"
                    }
                }
            }
        };

        try
        {
            await kubernetes.CreateNamespacedPodAsync(pod, namespaceName);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return false;
        }

        return true;
    }

    public async Task<string> CreateScenarioAsync(string name)
    {
        logger.LogInformation("Getting cluster information - client - CreateNamespaceAsync");
        var kubernetes = await client.LoadConfigurationAsync();
        logger.LogInformation("Creating namespace {NamespaceName}-test", name);
        var namespaceName = $"{name}-test";

        try
        {
            logger.LogInformation("Starting to create namespace");
            //1. create namespace trial-name
            await CreateNamespaceAsync(namespaceName);

            logger.LogInformation("Deploying deployment");
            //2. create deployment and service with load balancer)
            await kubernetes.CreateNamespacedDeploymentAsync(new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"{name}-deployment"
                },
                Kind = "Deployment",
                ApiVersion = "apps/v1",
                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string>
                        {
                            { "name", $"{name}-label" }
                        }
                    },
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string>
                            {
                                { "name", $"{name}-label" }
                            }
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = $"{name}-image",
                                    Image = Helpers.Constants.StoreImageName
                                }
                            }
                        }
                    }
                }
            }, namespaceName);

            logger.LogInformation("Creating service in a namespace {NamespaceName}", namespaceName);

            //3. create service
            await kubernetes.CreateNamespacedServiceAsync(new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"{name}-service"
                },
                Spec = new V1ServiceSpec
                {
                    Selector = new Dictionary<string, string>
                    {
                        { "name", $"{name}-label" }
                    },
                    Type = Helpers.Constants.ServiceTypeLoadBalancer,
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort
                        {
                            Port = 80,
                            Name = "default-port"
                        }
                    }
                }
            }, namespaceName);

            logger.LogInformation("Getting IP from newly created service");

            var service = kubernetes.ListNamespacedService(namespaceName);
            while (service.Items[0].Status.LoadBalancer.Ingress == null)
            {
                await Task.Delay(2000); // poor mans watch - wait for 2s and check again
                service = kubernetes.ListNamespacedService(namespaceName);
            }

            //4. get IP (of course here you can map it to external )
            return service.Items[0].Status.LoadBalancer.Ingress[0].Ip;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }

        return string.Empty;
    }

    public async Task<string> AssignDnsAsync(string uniqueDnsName, string ip, string rgName)
    {
        var subscriptionResource = azure.GetDefaultSubscription();
        var resourceGroup = subscriptionResource.GetResourceGroup(rgName).Value;
        var publicIPAddressContainer = resourceGroup.GetPublicIPAddresses();
        PublicIPAddressData ipAddress = null;
        foreach (var publicIpAddress in publicIPAddressContainer)
        {
            if (publicIpAddress.Data.IPAddress == ip) ipAddress = publicIpAddress.Data;
        }

        if (ipAddress == null) return string.Empty;

        ipAddress.DnsSettings = new PublicIPAddressDnsSettings
        {
            DomainNameLabel = uniqueDnsName
        };

        try
        {
            await publicIPAddressContainer.CreateOrUpdateAsync(WaitUntil.Completed, ipAddress.Name, ipAddress);
            var data = resourceGroup.GetPublicIPAddress(ipAddress.Name);//get refreshed version
            return data.Value.HasData ? data.Value.Data.DnsSettings?.Fqdn : string.Empty; //return FQDN or empty string
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return string.Empty;
        }
    }

    public async Task<bool> DeleteScenarioAsync(string name)
    {
        logger.LogInformation("Getting cluster information - client - DeleteScenarioAsync");
        var kubernetes = await client.LoadConfigurationAsync();
        logger.LogInformation("Deleting namespace {NamespaceName} (and with that everything)", name);

        var status = await kubernetes.DeleteNamespaceAsync($"{name}-test", new V1DeleteOptions());
        return status.Status == Helpers.Constants.SUCCESS;
    }
}