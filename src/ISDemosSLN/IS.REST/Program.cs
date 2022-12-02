using System.Diagnostics;
using IS.REST;
using Newtonsoft.Json;
using Spectre.Console;

Console.WriteLine("Calling REST to get back the information!");

using var client = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});
var clusterApiUrl = Environment.GetEnvironmentVariable("APIMASTERURL") ?? "https://localhost:8090";
AnsiConsole.WriteLine($"Reading from {clusterApiUrl}");
client.BaseAddress = new Uri(clusterApiUrl, UriKind.RelativeOrAbsolute);
client.DefaultRequestHeaders.Add("Accept", "application/json");

var bearerToken = Environment.GetEnvironmentVariable("BearerToken");

if (string.IsNullOrEmpty(bearerToken))
{
    AnsiConsole.WriteException(new UnauthorizedAccessException("Missing bearer token"), ExceptionFormats.ShowLinks);
    return;
}

var namespaceName = AnsiConsole.Ask<string>("Provide [green]namespace[/] name to traverse through pods", "default");
AnsiConsole.MarkupLine("Your defined: [yellow]{0}[/]", namespaceName);
var requestData = new HttpRequestMessage
{
    Method = HttpMethod.Get,
    RequestUri = new Uri($"{clusterApiUrl}/api/v1/namespaces/{namespaceName}/pods", UriKind.RelativeOrAbsolute)
};
requestData.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerToken}");

var result = await client.SendAsync(requestData);
if (!result.IsSuccessStatusCode)
{
    AnsiConsole.WriteLine("There has been an error accessing cluster, check content for more detais");
    AnsiConsole.WriteLine(result.ReasonPhrase);
    return;
}

var podsJson = await result.Content.ReadAsStringAsync();
Debug.WriteLine("Pods in JSON format: " + podsJson);

var pods = JsonConvert.DeserializeObject<Pods>(podsJson);
if (pods == null)
{
    AnsiConsole.WriteException(new NotSupportedException("Json is not in the right format. Check return value"));
    return;
}

var table = new Table();
table.Border(TableBorder.Ascii2);

table.AddColumn(new TableColumn("Pod name").Centered());
table.AddColumn(new TableColumn("Container used").Centered());

foreach (var pod in pods.Items)
{
    var containerDetails = "";
    foreach (var specContainer in pod.Spec.Containers)
    {
        containerDetails += $"{specContainer.Image}{Environment.NewLine}";
        if (specContainer.Ports != null)
        {
            foreach (var specContainerPort in specContainer.Ports)
            {
                containerDetails +=
                    $"accessible via {specContainerPort.Protocol} via port {specContainerPort.ContainerPort}{Environment.NewLine}";
            }
        }
    }

    table.AddRow(pod.Metadata.Name, containerDetails);
}

AnsiConsole.Write(table);