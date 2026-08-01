# deploy.ps1
# Deploys the current local branch to the live server: push to GitHub, then SSH in to
# pull/build/restart. Does not touch character save files - player data on the live
# server and local test saves are kept completely separate on purpose.

$ErrorActionPreference = "Stop"

$key = "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem"
$server = "ubuntu@18.222.119.157"
$branch = "linux-porting-fixes"

Write-Host "==> Pushing local changes to GitHub ($branch)..."
git push origin $branch
if ($LASTEXITCODE -ne 0) {
    Write-Host "Push failed, aborting deploy." -ForegroundColor Red
    exit 1
}

Write-Host "==> Pulling and rebuilding on the live server..."
$remoteCommand = @"
set -e
cd ~/Chaos-Server
git pull origin $branch
/home/ubuntu/.dotnet/dotnet build Chaos/Chaos.csproj -c Release
sudo systemctl restart chaos-server.service
sleep 3
sudo systemctl is-active chaos-server.service
"@

ssh -i $key $server $remoteCommand
if ($LASTEXITCODE -ne 0) {
    Write-Host "Remote deploy step failed - check the service status/logs on the server before assuming it's live." -ForegroundColor Red
    exit 1
}

Write-Host "Deploy complete!" -ForegroundColor Green
