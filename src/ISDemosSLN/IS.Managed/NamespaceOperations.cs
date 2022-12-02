using System.Net;
using k8s;
using k8s.Models;
using Spectre.Console;

namespace IS.Managed;

public class NamespaceOperations : BaseKubernetesOps
{
    public NamespaceOperations(IKubernetes client) : base(client)
    {
    }

    public async Task<string[]> GetNamespacesAsync()
    {
        var namespaces = await client.CoreV1.ListNamespaceAsync();
        return namespaces.Items
            .Select(currentItem => currentItem.Metadata.Name)
            .ToArray();
    }

    public async Task ListAllNamespacesAsync()
    {
        var namespaces = await client.CoreV1.ListNamespaceAsync();

        var table = new Table();
        table.AddColumn(new TableColumn("UID").Centered());
        table.AddColumn(new TableColumn("Name").Centered());
        table.AddColumn(new TableColumn("Labels").Centered());

        foreach (var ns in namespaces.Items)
        {
            var labels = string.Empty;
            if (ns.Metadata.Labels != null)
                foreach (var (name, value) in ns.Metadata.Labels)
                {
                    labels += $"{name}:{value}{Environment.NewLine}";
                }

            table.AddRow(ns.Metadata.Uid, ns.Metadata.Name, labels);
        }

        AnsiConsole.Write(table);
    }

    public async Task CreateNamespaceAsync(string name, IDictionary<string, string> labels)
    {
        var nsToCreate = new V1Namespace { Metadata = new V1ObjectMeta { Name = name, Labels = labels } };

        try
        {
            var createdNS = await client.CoreV1.CreateNamespaceAsync(nsToCreate);
            AnsiConsole.WriteLine(
                $"Namespace {createdNS.Metadata.Name} has been created at {createdNS.Metadata.CreationTimestamp ?? DateTime.Now}");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
    }

    public async Task DeleteNamespacesAsync(string name)
    {
        var status = await client.CoreV1.DeleteNamespaceAsync(name, new V1DeleteOptions());

        await AnsiConsole.Status()
            .AutoRefresh(false)
            .Spinner(Spinner.Known.Clock)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync($"Deleting namespace {name}...", async ctx =>
            {
                var retries = 1;
                if (status.HasObject)
                {
                    var obj = status.ObjectView<V1Namespace>();
                    ctx.Status(obj.Status.Phase);
                    ctx.Refresh();
                    retries = await DeleteNamespaceStatusAsync(ctx, name, 2000);
                }
                else
                    ctx.Status(status.Message);

                AnsiConsole.WriteLine($"Namespace {name} has been deleted after {retries} retries!");
            });
    }

    async Task<int> DeleteNamespaceStatusAsync(StatusContext statusContext, string name, int delayMillis)
    {
        var retries = 1;
        await Task.Delay(delayMillis).ConfigureAwait(false);
        try
        {
            statusContext.Status($"Checking if namespace {name} still exists: try {retries++}");
            statusContext.Refresh();
            await client.CoreV1.ReadNamespaceAsync(name).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                if (innerEx is k8s.Autorest.HttpOperationException exception)
                {
                    var code = exception.Response.StatusCode;
                    if (code == HttpStatusCode.NotFound)
                        return retries;
                }
            }
        }
        catch (k8s.Autorest.HttpOperationException ex)
        {
            if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                return retries;
        }

        statusContext.Refresh();
        return await DeleteNamespaceStatusAsync(statusContext, name, delayMillis);
    }
}