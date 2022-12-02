## Execute script line by line to to have Azure ACR ready for use

# get subscriptions and set the right one
az account list -o table
az account set --subscription "<subscription-id>"

# get AKS cluster list
az aks list -o table

# create ACR
az acr create --resource-group "copiedfromupperlist" --name "yourname" --sku Basic

# login to ACR
az acr login --name "<registry-name>"

# import image for testing purposes
az acr import --name myregistry --source docker.io/library/hello-world:latest --image hello-world:latest

# list container images
az acr repository list --name "<registry-name>" --output table

# you should see hello:world latest in the list

# attach acr to AKS cluster
az aks update -n "myAKSCluster" -g "myResourceGroup" --attach-acr "<acr-name>"

# test out functionality
az aks get-credentials -g "myResourceGroup" -n "myAKSCluster"

# check, if your cluster can pull from ACR
az aks check-acr --name MyManagedCluster --resource-group MyResourceGroup --acr myacr.azurecr.io

# if you don't have kubectl installed, do it via az cli
az aks install-cli

# create namespace to test, if it works inside cluster
kubectl create namespace test

# change values in this file 
kubectl apply -f 1-acr-basic-setup-script.yaml

# check results
kubectl get pods -n test

#after the deployment delete namespace
kubectl delete namespace test