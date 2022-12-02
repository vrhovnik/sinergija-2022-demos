## Execute script line by line to have azure aks ready

# login, get subscriptions associated with account 
az login
az account list -o table
# if you have multiple accounts
az account set --subscription "<subscription-id>"

# create resource group in specific location
 az account list-locations -o table
az group create --name "<resource-group-name>" --location "<location>"

# create AKS cluster with previously created resource group
az aks create --resource-group "<resource-group-name>" --name "<cluster-name>" --node-count 2 --generate-ssh-keys

# get AKS cluster list
az aks list -o table

# get AKS nodes to see if it is ready
az aks nodepool list -g "<resource-group-name>" -n "<cluster-name>" -o table

# install tools for working with cluster
az aks install-cli 

# get AKS cluster credentials .kube/config to be working with that cluster
az aks get-credentials -g "<resource-group-name>" -n "<cluster-name>"

## CLEAN UP after you finished with testing
# delete resource group and cluster
# az group delete -n "<resource-group-name>" --yes
 