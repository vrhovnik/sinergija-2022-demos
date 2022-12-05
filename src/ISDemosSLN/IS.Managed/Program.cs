using Bogus;
using IS.Managed;
using k8s;
using Spectre.Console;

AnsiConsole.MarkupLine(
    $"[link=https://github.com/vrhovnik/sinergija-2022-demos]Demo for working with Managed C# Kubernetes Api[/]!");
AnsiConsole.WriteLine("Loading from:");
AnsiConsole.Write(new TextPath(@"C:\Users\bovrhovn\.kube\config")
    .RootStyle(new Style(foreground: Color.Red))
    .SeparatorStyle(new Style(foreground: Color.Green))
    .StemStyle(new Style(foreground: Color.Blue))
    .LeafStyle(new Style(foreground: Color.Yellow)));

var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
IKubernetes client = new Kubernetes(config);
AnsiConsole.WriteLine($"Listening to API master at {config.Host}");
// menu selection
var menuSelection = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select demo action?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more menu options)[/]")
        .AddChoices("01 - Namespace operations and demos",
            "02 - workloads operations",
            "03 - use watch option",
            "04 - load yaml and do modifications",
            "05 - exec into POD",
            "06 - do port forwarding to a pod",
            "07 - get metrics for node and pods"));

var namespaceOps = new NamespaceOperations(client);
var workloadOps = new WorkloadOperations(client);
switch (menuSelection)
{
    case "01 - Namespace operations and demos":
    {
        HorizontalRule("01 - Namespace operations and demos");

        await namespaceOps.ListAllNamespacesAsync();

        var nsName = AnsiConsole.Ask<string>("What [green]namespace[/] would you like to create?");
        await namespaceOps.CreateNamespaceAsync(nsName,
            new Dictionary<string, string> { { "app", "cli" }, { "conf", "sinergija" }, { "type", "ns" } });

        await namespaceOps.ListAllNamespacesAsync();

        if (AnsiConsole.Confirm($"Delete namespace {nsName}?")) await namespaceOps.DeleteNamespacesAsync(nsName);
        break;
    }
    case "02 - workloads operations":
    {
        HorizontalRule("02 - workloads operations");

        var namespaceList = await namespaceOps.GetNamespacesAsync();
        var namespaceToCheckPods = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Pick [green]namespace[/] to get pods?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more namespaces)[/]")
                .AddChoices(namespaceList));

        await workloadOps.OutputPodsAsync(namespaceToCheckPods);

        var podImage = AnsiConsole.Ask<string>("What [green]image[/] would you like to use for creating the pod?");

        var podName = new Faker().Hacker.Abbreviation().ToLowerInvariant();

        await workloadOps.CreatePodAsync(podName, podImage,
            new Dictionary<string, string> { { "app", "cli" }, { "conf", "sinergija" }, { "type", "pods" } },
            namespaceToCheckPods);

        await workloadOps.OutputPodsAsync(namespaceToCheckPods);
        break;
    }
    case "03 - use watch option":
    {
        HorizontalRule("03 - use watch option");
        var namespaceList = await namespaceOps.GetNamespacesAsync();
        var namespaceToCheckPods = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Pick [green]namespace[/] to get pods?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more namespaces)[/]")
                .AddChoices(namespaceList));
        await workloadOps.GetPodsWithWatchEnabledAsync(namespaceToCheckPods);
        break;
    }
    case "04 - load yaml and do modifications":
    {
        //open new PWSH terminal and delete the pod kubectl delete pod nameofthepod -n namespacetocheckpods
        //return back here and press CTRL + C to continue
        HorizontalRule("04 - load yaml and do modifications");
        var checkAndRunResources = AnsiConsole.Confirm("Read and create [green]resources[/]?");
        await workloadOps.LoadYamlOutputDataAsync(checkAndRunResources);
        break;
    }
    case "05 - exec into POD":
    {
        HorizontalRule("05 - exec into POD");
        var podNameToExecInto =
            AnsiConsole.Ask<string>("Specify [green]name[/] for the pod to exe into", "simple-web-app");
        await workloadOps.CreatePodAsync(podNameToExecInto, "csacoreimages.azurecr.io/tta/web:1.0",
            new Dictionary<string, string> { { "app", "cli" }, { "conf", "sinergija" }, { "type", "pods" } });

        await Task.Delay(2000); //delay operation for k8s to finish with processing
        
        var podToExecInto = await workloadOps.GetV1PodAsync(podNameToExecInto);
        AnsiConsole.WriteLine($"Read pod {podToExecInto.Metadata.Name}");

        await workloadOps.ExecIntoPodAsync(podToExecInto);
        break;
    }
    case "06 - do port forwarding to a pod":
        HorizontalRule("06 - do port forwarding to a pod");
        var podNamePortForward =
            AnsiConsole.Ask<string>("Specify [green]name[/] for the pod to port fwd to", "simple-web-app-for-exec");
        await workloadOps.CreatePodAsync(podNamePortForward, "csacoreimages.azurecr.io/tta/web:1.0",
            new Dictionary<string, string> { { "app", "cli" }, { "conf", "sinergija" }, { "type", "pods" } });

        await Task.Delay(2000);
        var podToForwardTo = await workloadOps.GetV1PodAsync(podNamePortForward);
        await workloadOps.PortforwardToPodAsync(podToForwardTo);
        break;
    case "07 - get metrics for node and pods":
        HorizontalRule("07 - get metrics for node and pods");
        await workloadOps.GetNodesMetricsAsync();
        Console.Read();
        await workloadOps.GetPodsMetricsAsync();
        break;
    default:
        AnsiConsole.WriteLine("No menu with action selected");
        break;
}

void HorizontalRule(string title)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule($"[white bold]{title}[/]").RuleStyle("grey").LeftAligned());
    AnsiConsole.WriteLine();
}