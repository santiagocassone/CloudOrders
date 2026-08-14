param(
	[Parameter(Mandatory = $false)]
	[string]$ServiceBusConnectionString = $env:ServiceBus__ConnectionString,

	[Parameter(Mandatory = $false)]
	[string]$QueueName = "order-placed",

	[Parameter(Mandatory = $false)]
	[Guid]$OrderId = [Guid]::NewGuid(),

	[Parameter(Mandatory = $false)]
	[Guid]$ProductId = [Guid]::Parse("11111111-1111-1111-1111-111111111111"),

	[Parameter(Mandatory = $false)]
	[int]$Quantity = 2,

	[Parameter(Mandatory = $false)]
	[int]$TokenTtlSeconds = 3600
)

$ErrorActionPreference = "Stop"

function Get-ConnectionStringValue([string]$connectionString, [string]$key) {
	$parts = $connectionString.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)
	foreach ($part in $parts) {
		$kv = $part.Split('=', 2)
		if ($kv.Length -eq 2 -and $kv[0] -eq $key) {
			return $kv[1]
		}
	}

	return $null
}

function New-SasToken([string]$resourceUri, [string]$keyName, [string]$key, [int]$ttlSeconds) {
	$expiry = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + $ttlSeconds
	$encodedResource = [System.Net.WebUtility]::UrlEncode($resourceUri.ToLowerInvariant())
	$toSign = "{0}`n{1}" -f $encodedResource, $expiry

	$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($key))
	try {
		$signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($toSign))
	}
	finally {
		$hmac.Dispose()
	}

	$signature = [System.Net.WebUtility]::UrlEncode([Convert]::ToBase64String($signatureBytes))
	return "SharedAccessSignature sr=$encodedResource&sig=$signature&se=$expiry&skn=$keyName"
}

if ([string]::IsNullOrWhiteSpace($ServiceBusConnectionString)) {
	throw "Service Bus connection string is required. Pass -ServiceBusConnectionString or set ServiceBus__ConnectionString env var."
}

$endpoint = Get-ConnectionStringValue -connectionString $ServiceBusConnectionString -key "Endpoint"
$keyName = Get-ConnectionStringValue -connectionString $ServiceBusConnectionString -key "SharedAccessKeyName"
$key = Get-ConnectionStringValue -connectionString $ServiceBusConnectionString -key "SharedAccessKey"

if ([string]::IsNullOrWhiteSpace($endpoint) -or [string]::IsNullOrWhiteSpace($keyName) -or [string]::IsNullOrWhiteSpace($key)) {
	throw "Invalid Service Bus connection string. Expected Endpoint, SharedAccessKeyName, and SharedAccessKey."
}

$baseUri = $endpoint.Replace("sb://", "https://").TrimEnd('/')
$queueUri = "$baseUri/$QueueName"
$sendUri = "$queueUri/messages"

$authorization = New-SasToken -resourceUri $queueUri -keyName $keyName -key $key -ttlSeconds $TokenTtlSeconds

$payload = @{
	orderId = $OrderId
	items = @(
		@{
			productId = $ProductId
			quantity = $Quantity
		}
	)
}

$brokerProperties = @{
	MessageId = [Guid]::NewGuid().ToString("N")
	CorrelationId = $OrderId.ToString()
	ContentType = "application/json"
} | ConvertTo-Json -Compress

$headers = @{
	Authorization = $authorization
	BrokerProperties = $brokerProperties
}

$body = $payload | ConvertTo-Json -Depth 6

Write-Host "Sending OrderPlaced to queue '$QueueName'..." -ForegroundColor Cyan
Invoke-RestMethod -Method Post -Uri $sendUri -Headers $headers -ContentType "application/json" -Body $body | Out-Null

Write-Host "Sent successfully." -ForegroundColor Green
Write-Host "OrderId: $OrderId"
Write-Host "Queue: $QueueName"
Write-Host "Namespace: $baseUri"
