using IS.Web.Models;
using k8s;

namespace IS.Web.Interfaces;

public interface IKubernetesService
{
    Task<KCInfo> GetInfoAsync();
    Task<Kubernetes> LoadConfigurationAsync();
}