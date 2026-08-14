$env:PATH = "C:\Users\Josh\dotnet;" + $env:PATH
cd C:\dev\DubboSDR\src\DubboSDR.Diagnostics

Write-Host "Rebuilding Diagnostics..."
dotnet build -c Release

Write-Host "`n=== Testing 93.5 MHz ==="
dotnet run -c Release -- 93500000

Write-Host "`n=== Testing 102.3 MHz ==="
dotnet run -c Release -- 102300000

Write-Host "`n=== Testing 105.5 MHz ==="
dotnet run -c Release -- 105500000

Write-Host "`n=== Testing 107.9 MHz ==="
dotnet run -c Release -- 107900000

Write-Host "`n=== Running Band Sweep ==="
dotnet run -c Release -- --scan
