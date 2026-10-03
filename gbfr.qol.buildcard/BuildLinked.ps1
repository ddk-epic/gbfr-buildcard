# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/gbfr.qol.buildcard/*" -Force -Recurse
dotnet publish "./gbfr.qol.buildcard.csproj" -c Release -o "$env:RELOADEDIIMODS/gbfr.qol.buildcard" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location