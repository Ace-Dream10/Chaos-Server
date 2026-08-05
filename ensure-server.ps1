# ensure-server.ps1
# Convenience tool: checks whether the local Elysium server is already listening on its
# three ports (4200 Lobby, 4201 Login, 4202 World - see Chaos/appsettings.json) and starts
# it if not. Not part of the game itself - keep this simple.

$ErrorActionPreference = "Stop"

$ports = @(
    @{ Name = "Lobby"; Port = 4200 },
    @{ Name = "Login"; Port = 4201 },
    @{ Name = "World"; Port = 4202 }
)

function Test-ServerPorts {
    foreach ($p in $ports) {
        # No -LocalAddress filter: the server binds 0.0.0.0 (all interfaces), not specifically
        # 127.0.0.1, so a listener bound to 0.0.0.0 never matches a 127.0.0.1-scoped query even
        # though it's still reachable over loopback. Filtering by port + Listen state alone is
        # what actually detects "is this port already occupied."
        $listening = Get-NetTCPConnection -LocalPort $p.Port -State Listen -ErrorAction SilentlyContinue
        if (-not $listening) {
            return $false
        }
    }
    return $true
}

if (Test-ServerPorts) {
    Write-Host "Server already running on 4200/4201/4202 (Lobby/Login/World) - not starting a second instance." -ForegroundColor Green
    exit 0
}

Write-Host "Server not detected on 4200/4201/4202 - starting it..." -ForegroundColor Yellow

# Launched detached in its own window rather than blocking this shell: this script is a "just
# make sure it's up" utility, so it should hand control back to the caller immediately. The
# tradeoff is you don't see this script's own console reflect the server's live log output -
# but the new window itself does, so nothing is actually hidden, just relocated.
$projectRoot = $PSScriptRoot
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "Chaos/Chaos.csproj" -WorkingDirectory $projectRoot -WindowStyle Normal

Write-Host "Waiting for ports to come up (dotnet run has a real build/startup delay)..."

$maxRetries = 30
$retryDelaySecs = 3
$upAfterStart = $false

for ($i = 1; $i -le $maxRetries; $i++) {
    Start-Sleep -Seconds $retryDelaySecs
    if (Test-ServerPorts) {
        $upAfterStart = $true
        break
    }
    Write-Host "  still waiting... ($i/$maxRetries)"
}

if ($upAfterStart) {
    Write-Host "Server started successfully - 4200/4201/4202 (Lobby/Login/World) are now listening." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Server process was launched, but ports 4200/4201/4202 did not come up within $($maxRetries * $retryDelaySecs)s - check the new console window for build/startup errors." -ForegroundColor Red
    exit 1
}
