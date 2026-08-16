param(
	[string]$SubscriptionId = "",
	[string]$ResourceGroupName = "rg-cloudorders-inventory-prod",
	[string]$Location = "eastus",
	[string]$ServiceBusNamespace = "cloudorders-prod-escasan",
	[string]$OrderPlacedQueueName = "order-placed",
	[string]$StockResultsQueueName = "stock-results"
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$message) {
	Write-Host "[INFO] $message" -ForegroundColor Cyan
}

function Ensure-AzureCli {
	if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
		throw "Azure CLI is not installed. Install from https://learn.microsoft.com/cli/azure/install-azure-cli"
	}
}

function Ensure-LoggedIn {
	try {
		$null = az account show --output none 2>$null
	}
	catch {
		throw "No active Azure login context found. Run 'az login' first."
	}
}

Ensure-AzureCli
Ensure-LoggedIn

if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
	Write-Info "Setting subscription to '$SubscriptionId'..."
	az account set --subscription $SubscriptionId
}

Write-Info "Ensuring resource group '$ResourceGroupName' in '$Location'..."
az group create --name $ResourceGroupName --location $Location --output table

Write-Info "Ensuring Service Bus namespace '$ServiceBusNamespace'..."
$namespaceExists = az servicebus namespace exists --name $ServiceBusNamespace --output tsv
if ($namespaceExists -ne "true") {
	az servicebus namespace create --resource-group $ResourceGroupName --name $ServiceBusNamespace --location $Location --sku Standard --output table
}
else {
	Write-Info "Namespace already exists."
}

Write-Info "Ensuring queue '$OrderPlacedQueueName'..."
$queue1Exists = az servicebus queue exists --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --name $OrderPlacedQueueName --output tsv
if ($queue1Exists -ne "true") {
	az servicebus queue create --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --name $OrderPlacedQueueName --output table
}
else {
	Write-Info "Queue '$OrderPlacedQueueName' already exists."
}

Write-Info "Ensuring queue '$StockResultsQueueName'..."
$queue2Exists = az servicebus queue exists --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --name $StockResultsQueueName --output tsv
if ($queue2Exists -ne "true") {
	az servicebus queue create --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --name $StockResultsQueueName --output table
}
else {
	Write-Info "Queue '$StockResultsQueueName' already exists."
}

Write-Info "Done."
Write-Host "Service Bus namespace: $ServiceBusNamespace.servicebus.windows.net"
Write-Host "Inbound queue (CloudOrders -> Inventory): $OrderPlacedQueueName"
Write-Host "Outbound queue (Inventory -> CloudOrders): $StockResultsQueueName"
