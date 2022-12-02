using System.Diagnostics;
using k8s;
using k8s.Models;
using Spectre.Console;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IS.Managed;

public class WorkloadOperations : BaseKubernetesOps
{
    public delegate void WatchActionDelegate(WatchActionArgs args);

    public event WatchActionDelegate PodStatusChanged;


    public WorkloadOperations(IKubernetes client) : base(client)
    {
    }

    public async Task GetPodsWithWatchEnabledAsync(string namespaceToCheck = "default")
    {
        var podlistResp = await client.CoreV1.ListNamespacedPodWithHttpMessagesAsync(namespaceToCheck, watch: true);
        using (podlistResp.Watch<V1Pod, V1PodList>((type, item) =>
               {
                   if (PodStatusChanged != null)
                   {
                       var args = new WatchActionArgs(type.ToString(), item.Metadata.Name);
                       PodStatusChanged(args);
                       if (args.Cancel)
                       {
                           AnsiConsole.WriteLine("Event has been canceled");
                           return;
                       }
                   }
                   
                   AnsiConsole.WriteLine("Watching pods - kubectl get po --watch");
                   AnsiConsole.Write(new Markup($"[red]{type.ToString()}[/] with pod [green]{item.Metadata.Name}[/]"));
                   AnsiConsole.WriteLine("Waiting for next stats");
               }))
        {
            AnsiConsole.WriteLine("Press ctrl + c to stop watching");

            var ctrlc = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, _) => ctrlc.Set();
            ctrlc.Wait();
        }
    }

    public async Task ExecIntoPodAsync(V1Pod pod, string commandToExecute = "ls", string namespaceName = "default")
    {
        var webSocket =
            await client.WebSocketNamespacedPodExecAsync(pod.Metadata.Name, namespaceName, commandToExecute,
                pod.Spec.Containers[0].Name).ConfigureAwait(false);

        var demux = new StreamDemuxer(webSocket);
        demux.Start();

        var buff = new byte[4096];
        var stream = demux.GetStream(1, 1);
        await stream.ReadAsync(buff.AsMemory(0, 4096));
        AnsiConsole.WriteLine($"Output from pod {pod.Metadata.Name}:");
        var str = Encoding.Default.GetString(buff);
        AnsiConsole.WriteLine(str);
    }

    public async Task PortforwardToPodAsync(V1Pod pod, string namespaceName = "default")
    {
        var webSocket = await client.WebSocketNamespacedPodPortForwardAsync(pod.Metadata.Name,
            namespaceName,
            new[] { 80 }, "v4.channel.k8s.io");
        var demux = new StreamDemuxer(webSocket, StreamType.PortForward);
        demux.Start();

        var stream = demux.GetStream((byte?)0, (byte?)0);

        var ipAddress = IPAddress.Loopback;
        var localEndPoint = new IPEndPoint(ipAddress, 8080);
        var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(100);

        Socket handler = null;

        var accept = Task.Run(() =>
        {
            while (true)
            {
                handler = listener.Accept();
                var bytes = new byte[4096];
                while (true)
                {
                    var bytesRec = handler.Receive(bytes);
                    stream.Write(bytes, 0, bytesRec);
                    if (bytesRec == 0 || Encoding.ASCII.GetString(bytes, 0, bytesRec).IndexOf("<EOF>") > -1)
                        break;
                }
            }
        });

        var copy = Task.Run(() =>
        {
            var buff = new byte[4096];
            while (true)
            {
                var read = stream.Read(buff, 0, 4096);
                handler.Send(buff, read, 0);
            }
        });

        await accept;
        await copy;

        handler?.Close();
        listener.Close();
    }

    public async Task<V1Pod> GetV1PodAsync(string name, string namespaceName = "default")
    {
        try
        {
            return await client.CoreV1.ReadNamespacedPodAsync(name, namespaceName);
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        return null;
    }

    public async Task LoadYamlOutputDataAsync(bool execute = false)
    {
        var filePath = Environment.GetEnvironmentVariable("YAMLPATH") ??
                       @"C:\Work\Projects\ntk-2022-demos\yaml";

        if (!Directory.Exists(filePath))
        {
            AnsiConsole.WriteException(new DirectoryNotFoundException("That path is not a directory"));
            return;
        }

        AnsiConsole.WriteLine("YAML file directory:");
        AnsiConsole.Write(new TextPath(filePath)
            .RootStyle(new Style(foreground: Color.Red))
            .SeparatorStyle(new Style(foreground: Color.Green))
            .StemStyle(new Style(foreground: Color.Blue))
            .LeafStyle(new Style(foreground: Color.Yellow)));

        var absolutePath = Path.Join(filePath, "simple-example.yaml");

        var typeMap = new Dictionary<string, Type>
        {
            { "v1/Pod", typeof(V1Pod) },
            { "v1/Service", typeof(V1Service) },
            { "apps/v1/Deployment", typeof(V1Deployment) }
        };

        AnsiConsole.WriteLine("Loading data from simple ");
        var objects = await KubernetesYaml.LoadAllFromFileAsync(absolutePath, typeMap);
        foreach (var obj in objects)
        {
            AnsiConsole.WriteLine(obj.ToString());
            if (execute)
            {
                switch (obj)
                {
                    case V1Service service:
                        await client.CoreV1.CreateNamespacedServiceAsync(service, "default");
                        break;
                    case V1Pod pod:
                        await client.CoreV1.CreateNamespacedPodAsync(pod, "default");
                        break;
                    case V1Deployment deployment:
                        //change connection string environment variable
                        deployment.Spec.Template.Spec.Containers[0].Env[0].Value =
                            Environment.GetEnvironmentVariable("PRODUCTIONSQLCONNSTRING");
                        await client.AppsV1.CreateNamespacedDeploymentAsync(deployment, "default");
                        break;
                }
            }
        }
    }

    public async Task CreatePodAsync(string name, string image, IDictionary<string, string> labels,
        string namespaceName = "default")
    {
        var v1Pod = new V1Pod
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                Labels = labels
            },
            Spec = new V1PodSpec
            {
                Containers = new List<V1Container>
                {
                    new(name, image: image)
                }
            }
        };

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        try
        {
            AnsiConsole.WriteLine($"Started creating pod {name} in namespace {namespaceName}..");
            await client.CoreV1.CreateNamespacedPodAsync(v1Pod, namespaceName);
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        stopWatch.Stop();

        AnsiConsole.WriteLine(
            $"Pod with name {name} and {image} has been created in {stopWatch.ElapsedMilliseconds} ms.");
    }

    public async Task OutputPodsAsync(string namespaceName = "default")
    {
        var podList = await client.CoreV1.ListNamespacedPodAsync(namespaceName);

        var table = new Table();
        table.AddColumn(new TableColumn("UID").Centered());
        table.AddColumn(new TableColumn("Name").Centered());
        table.AddColumn(new TableColumn("Labels").Centered());

        foreach (var podListItem in podList.Items)
        {
            var labels = string.Empty;
            if (podListItem.Metadata.Labels != null)
                foreach (var (name, value) in podListItem.Metadata.Labels)
                {
                    labels += $"{name}:{value}{Environment.NewLine}";
                }

            table.AddRow(podListItem.Metadata.Uid, podListItem.Metadata.Name, labels);
        }

        AnsiConsole.Write(table);
    }
}