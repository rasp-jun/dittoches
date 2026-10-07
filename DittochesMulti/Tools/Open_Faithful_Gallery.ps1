param([ValidatePattern('^[a-z]*$')][string]$Model = '', [ValidateSet('', 'Idle', 'Walk', 'Run', 'Attack', 'Skill', 'Guard', 'Dodge', 'Hit', 'Victory', 'Down')][string]$Motion = '', [switch]$Demo)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$galleryUrl = 'http://127.0.0.1:8766/'
$logFolder = Join-Path $projectRoot 'Builds/FaithfulGallery'
New-Item -ItemType Directory -Path $logFolder -Force | Out-Null

function Test-GalleryServer {
    try {
        $health = Invoke-RestMethod -Uri ($galleryUrl + 'health') -TimeoutSec 2
        return $health.app -eq 'faithful-digimon-gallery'
    } catch { return $false }
}

if (-not (Test-GalleryServer)) {
    $pythonCommand = Get-Command python.exe -ErrorAction SilentlyContinue
    $pythonPath = if ($pythonCommand) { $pythonCommand.Source } else { $null }
    $localPython = Join-Path $env:USERPROFILE 'miniconda3/python.exe'
    if (Test-Path -LiteralPath $localPython) { $pythonPath = $localPython }
    if (-not $pythonPath) { throw 'Python 3 is required to open this local preview.' }
    $scriptFile = Join-Path $PSScriptRoot 'serve_faithful_gallery.py'
    $serverProcess = Start-Process -FilePath $pythonPath -ArgumentList @('"' + $scriptFile + '"') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logFolder 'server.log') -RedirectStandardError (Join-Path $logFolder 'server-error.log')
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        if (Test-GalleryServer) { $ready = $true; break }
        if ($serverProcess.HasExited) { break }
        Start-Sleep -Milliseconds 200
    }
    if (-not $ready) { throw ('Preview server could not start. See ' + (Join-Path $logFolder 'server-error.log')) }
}

$openUrl = $galleryUrl
if ($Demo) { $openUrl += '?demo=1' }
if ($Motion) { $openUrl += $(if ($Demo) { '&' } else { '?' }) + 'motion=' + $Motion }
if ($Model) { $openUrl += '#' + $Model }
$edgeCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft/Edge/Application/msedge.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft/Edge/Application/msedge.exe')
)
$edgePath = $edgeCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($edgePath) {
    Start-Process -FilePath $edgePath -ArgumentList @('--new-window', '--window-size=1440,980', $openUrl) -WindowStyle Normal
} else {
    Start-Process $openUrl
}
Write-Host ('Opened local Digimon preview: ' + $openUrl)
