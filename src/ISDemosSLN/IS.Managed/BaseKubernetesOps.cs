using k8s;
using Spectre.Console;

namespace IS.Managed;

public abstract class BaseKubernetesOps
{
    internal readonly IKubernetes client;

    protected BaseKubernetesOps(IKubernetes client) => this.client = client;

    public async Task GetNodesMetricsAsync()
    {
        var nodesMetrics = await client.GetKubernetesNodesMetricsAsync().ConfigureAwait(false);

        var table = new Table();
        table.AddColumn(new TableColumn("Node").Centered());
        table.AddColumn(new TableColumn("Key").Centered());
        table.AddColumn(new TableColumn("Value").Centered());

        foreach (var item in nodesMetrics.Items)
        {
            AnsiConsole.WriteLine(item.Metadata.Name);

            foreach (var metric in item.Usage)
            {
                table.AddRow(item.Metadata.Name, metric.Key, metric.Value.ToString());
            }
        }
        AnsiConsole.Write(table);
    }

    public async Task GetPodsMetricsAsync()
    {
        var podsMetrics = await client.GetKubernetesPodsMetricsAsync().ConfigureAwait(false);

        if (!podsMetrics.Items.Any()) 
            AnsiConsole.WriteLine("No pod metrics are available.");

        var table = new Table();
        table.AddColumn(new TableColumn("Container").Centered());
        table.AddColumn(new TableColumn("Key").Centered());
        table.AddColumn(new TableColumn("Value").Centered());
        
        foreach (var item in podsMetrics.Items)
        {
            foreach (var container in item.Containers)
            {
                Console.WriteLine(container.Name);

                foreach (var metric in container.Usage)
                {
                    table.AddRow(container.Name, metric.Key, metric.Value.ToString());
                }
            }
        }
        AnsiConsole.Write(table);
    }
}