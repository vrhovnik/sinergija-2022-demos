namespace IS.Web.Helpers;

public static class Constants
{
    public const string SUCCESS = "Success";
    public const string FAILURE = "Failure";
    public const string StoreImageName = "csacoreimages.azurecr.io/tta/web:1.0";
    public const string DefaultServiceType = ServiceTypeLoadBalancer;
    public const string ServiceTypeLoadBalancer = "LoadBalancer";
    public const string ServiceTypeClusterIp = "ClusterIp";
    public const string ServiceTypeNone = "None";
    public const string KubernetesRGName = "MC_KubernetesRG_csaaks_westeurope";
}