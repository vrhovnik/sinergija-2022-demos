# Sinergija 2022 demos

Demos for conference [Sinergija 2022](https://www.sinergija.live/en/), December 2022 in Belgrade, Serbia. I will be
talking about how to write your own kubectl with Csharp, explaining possible options and what is already done in Kubectl
world to take advantage on integrating your workflow with Azure Kubernetes Service as main tech provider behind the scenes.

Slides are available [here](https://webeudatastorage.blob.core.windows.net/web/Programmatic-access-to-AKS.pdf), demo explanation below.

<!-- TOC -->
* [Sinergija 2022 - demos for session about Programmatic access to AKS](#sinergija-2022-conference---demos-for-session-about-programmatic-access-to-aks)
  * [Demo structure](#demo-structure)
    * [Csharp project structure](#csharp-project-structure)
    * [IS.REST project](#isrest-project)
    * [IS.Managed](#ismanaged)
    * [IS.Web](#isweb)
  * [Links](#links)
* [Credits](#credits)
* [QUESTIONS / COMMENTS](#questions--comments)
<!-- TOC -->

## Demo structure

Demo consist out of 2 major parts:

1. [Postman](https://getpostman.com) queries
2. CSharp project with code

Postman collection can be found [here](./scripts/Kubectl%20Session%20Empty.postman_collection.json). How to work with
Postman, follow this tutorial [here](https://learning.postman.com/docs/getting-started/importing-and-exporting-data/).

### Csharp project structure

![Project structure](https://webeudatastorage.blob.core.windows.net/web/is-demo-structure.png)

It contains console and web applications. To run it, you need to have:

1. [DotNet](https://dot.net) installed
2. [OPTIONAL] .NET IDE by your choice ([Visual Studio](https://visualstudio.com)
   . [Visual Studio Code](https://code.visualstudio.com) with
   with [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
   , [Jetbrains Rider](https://jetbrains.com/rider),...)

To run the app, navigate to the project folder in your terminal you want to run and execute dotnet run (or use IDE
editor ways to run app)

`cd [FOLDERROOT where you git clone'd]\src\ISDemosSLN\IS.REST\`

`dotnet run`

To help you with creating [Azure](https://azure.com) resources, you can refer step by step script to do that by
visiting [this folder](./scripts).

### IS.REST project

It needs [bootstrap token](https://kubernetes.io/docs/reference/access-authn-authz/bootstrap-tokens/) to authenticate
against API. You can use [Postman](https://www.postman.com/) or [curl](https://en.wikipedia.org/wiki/CURL) in order to
issue the command. To successfully run this project, you will need to provide **BearerToken** and **ClusterBaseAddress**
as [environment variables](https://en.wikipedia.org/wiki/Environment_variable).

The easiest way is to
create [service account](https://kubernetes.io/docs/reference/access-authn-authz/service-accounts-admin/) and then do
role binding on a cluster to define access levels. With that defined, you can then query the secret to get the token.
Use this
command `kubectl -n kube-system describe secret $(kubectl -n kube-system get secret | grep youraccountname | awk '{print $1}')`
.

### IS.Managed

It needs [kubeconfig file]((https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig)) in
order to run the application. On Linux check **.kube** hidden folder in home folder (on by default).

If you have [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) installed, use `kubectl config view` to
see the file.

![config view](https://webeudatastorage.blob.core.windows.net/web/meetup-config-view.png)

Solution will automatically load the default config file and authenticate against Kubernetes cluster.

### IS.Web

It uses [Azure AD authentication](https://azure.com/sdk) via managed library (C#) to authenticate with AAD to
access [Azure Kubernetes Service](https://docs.microsoft.com/en-us/azure/aks/). I am
using [Microsoft Identity Web authentication library](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
. [Flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-sign-in-flow) is explained here.

To do it step by
step, [follow](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals) this
tutorial. When you have the service principal, you will need to fill in
the [following details](https://github.com/bovrhovn/meetup-demo-kubectl-differently/blob/main/src/KubectlSLN/Kubectl.Web/appsettings.json)
in configuration setting (or add environment variables):

![settings](https://webeudatastorage.blob.core.windows.net/web/meetup-web-settings.png)

You can find the data in service principal details (created earlier) and Azure AD portal details page. As part of the
application, I am using [Azure Storage](https://docs.microsoft.com/en-us/azure/storage/) to store different config
files (in demo only one), you will need to fill in the details
about [Storage connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string?toc=/azure/storage/blobs/toc.json)
and container name.

If you want to get remote access to populated container images from a remote docker host (setting **DockerHostUrl**),
you can
follow [this tutorial here](https://docs.docker.com/engine/install/linux-postinstall/#configuring-remote-access-with-daemonjson)
in order to allow TCP connectivity and provide URL (IP) to the application to show image list.

For logging purposes I
use [Application Insight](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview). If you want
to measure performance and see detailed logs (and many more), follow this [tutorial](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core). You will need to provide **Instrumentation key** for app to send data and logs to AI. It is not mandatory for app to run.

## Links

Useful links to read about:

1. [AKS Docs](https://docs.microsoft.com/en-us/azure/aks)
2. [AKS Baseline from Microsoft Patterns & Practices team](https://github.com/mspnp/aks-baseline)
3. [AKS Deployment Helper](https://azure.github.io/AKS-Construction/)
4. [AKS Landing Zone Accelerator](https://github.com/Azure/AKS-Landing-Zone-Accelerator)
5. [Kubernetes Api Client Libraries](https://github.com/kubernetes-client)
   and [3rd party community-maintained client libraries](https://kubernetes.io/docs/reference/using-api/client-libraries/#community-maintained-client-libraries)
6. [Kubernetes Api Overview](https://kubernetes.io/docs/reference/using-api/)
   and [controlling access to cluster](https://kubernetes.io/docs/concepts/security/controlling-access/)
7. [Kubeconfig view](https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig/)
8. [Setup kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)
9. [Power tools for kubectl](https://github.com/ahmetb/kubectx)
10. [Portainer](https://www.portainer.io/installation/)
11. [Azure Kubernetes Service](https://docs.microsoft.com/en-us/azure/aks/)
12. [Application Insight](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

# Credits

In this demo, we used the following 3rd party libraries and solutions:

1. [Spectre Console](https://github.com/spectresystems/spectre.console/)
2. [C# managed library for Kubernetes](https://github.com/kubernetes-client/csharp)
3. [Portainer](https://www.portainer.io/installation/)
3. [Rancher](https://rancher.com/)

# QUESTIONS / COMMENTS

If you have any questions, comments, open an issue and happy to answer.

