param([string]$ListenAddress = '127.0.0.1', [int]$Port = 7777)
$ErrorActionPreference = 'Stop'
Write-Host 'Dittoches: 8-player normal / ranked league server'
if ($ListenAddress -eq '0.0.0.0') {
    Write-Host 'Other PCs on this network can connect using one of these addresses:'
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -ne '127.0.0.1' -and $_.IPAddress -notlike '169.254.*' } |
        ForEach-Object { Write-Host ("  http://" + $_.IPAddress + ":" + $Port) }
} else { Write-Host ("Local address: http://" + $ListenAddress + ":" + $Port) }
$pythonExe = Join-Path $env:LOCALAPPDATA 'Python\pythoncore-3.14-64\python.exe'
if (Test-Path -LiteralPath $pythonExe) {
    & $pythonExe (Join-Path $PSScriptRoot 'server.py') --host $ListenAddress --port $Port
} else {
    python (Join-Path $PSScriptRoot 'server.py') --host $ListenAddress --port $Port
}
