using k8s.Models;

namespace IS.Web.Interfaces;

public interface IKubernetesObjects
{
    Task<IEnumerable<V1Namespace>> ListNamespacesAsync();
    Task<IEnumerable<V1Pod>> ListPodsAsync(string namespaceName = "default");
}