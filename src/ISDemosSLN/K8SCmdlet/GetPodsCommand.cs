using System.Management.Automation;
using k8s;

namespace K8SCmdlet;

[Cmdlet(VerbsCommon.Get, "Pods")]
public class GetPodsCommand : Cmdlet
{
    [Parameter(Position = 0, ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string? NamespaceName { get; set; }

    protected override void ProcessRecord()
    {
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
        IKubernetes client = new Kubernetes(config);
        
        if (string.IsNullOrEmpty(NamespaceName)) NamespaceName = "default";
        
        var podList = client.CoreV1.ListNamespacedPod(NamespaceName);

        foreach (var currentPod in podList.Items)
        {
            WriteObject(currentPod, true);
        }

        base.ProcessRecord();
    }
}