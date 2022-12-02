using Azure.ResourceManager.ContainerRegistry;
using IS.Web.Models;

namespace IS.Web.Interfaces;

public interface IContainerRegistryService
{
    Task<ContainerRegistryResource> GetRegistryRepositoriesAsync(string containerRegistryName);
    Task<List<DockerImageViewModel>> GetImagesForRepositoryAsync(string containerRegistryName);
    List<DockerImageViewModel> GetPredefinedImages();
}