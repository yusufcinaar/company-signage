#requires -version 5.1
param(
    [string]$OutputRoot = "$PSScriptRoot\..\artifacts"
)
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path "$PSScriptRoot\..").Path
$serverOut = Join-Path $OutputRoot 'Server'
$playerOut = Join-Path $OutputRoot 'PlayerWin7'
$agentOut = Join-Path $OutputRoot 'AgentWin7'

New-Item -ItemType Directory -Force $serverOut,$playerOut,$agentOut | Out-Null
$staleAgentFolder = Join-Path $playerOut 'Agent'
if (Test-Path -LiteralPath $staleAgentFolder) {
    Remove-Item -LiteralPath $staleAgentFolder -Recurse -Force
}

function Invoke-Publish([string]$Project, [string[]]$Arguments) {
    & dotnet publish $Project @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Yayınlama başarısız: $Project (kod $LASTEXITCODE)"
    }
}

Invoke-Publish "$repo\src\CompanySignage.Web\CompanySignage.Web.csproj" @(
    '-c','Release','-r','win-x64','--self-contained','true',
    '-m:1','-o',$serverOut
)
Invoke-Publish "$repo\src\CompanySignage.Player\CompanySignage.Player.csproj" @(
    '-c','Release','-m:1','-o',$playerOut
)
Invoke-Publish "$repo\src\CompanySignage.Agent.Win7\CompanySignage.Agent.Win7.csproj" @(
    '-c','Release','-m:1','-o',$agentOut
)
Copy-Item "$repo\scripts\install-server.ps1" $serverOut -Force
Copy-Item "$repo\scripts\install-player-win7.ps1" $playerOut -Force
Copy-Item "$repo\scripts\KURULUM-BASLAT.bat" $playerOut -Force

$agentPackageFolder = New-Item -ItemType Directory -Force (Join-Path $playerOut 'Agent')
Copy-Item "$agentOut\*" $agentPackageFolder -Force

$installNote = @"
1. ZIP dosyasını ekran bilgisayarında bir klasöre çıkarın.
2. KURULUM-BASLAT.bat dosyasına çift tıklayın.
3. Sorulan sunucu adresi, ekran kodu ve token bilgilerini girin.
4. Kurulum .NET Framework 4.8 eksikse Microsoft'tan otomatik indirir.
5. Yeniden başlatma istenirse bilgisayarı yeniden başlatıp BAT dosyasını tekrar çalıştırın.

Alternatif PowerShell komutu:
powershell -ExecutionPolicy Bypass -File .\install-player-win7.ps1 -ServerUrl "http://SUNUCU_IP:5000" -ScreenCode "SCREEN-001" -DeviceToken "EKRAN_TOKEN"
"@
Set-Content (Join-Path $playerOut 'KURULUM.txt') $installNote -Encoding utf8

$packagesOut = New-Item -ItemType Directory -Force (Join-Path $serverOut 'Packages')
$installerZip = Join-Path $packagesOut 'CompanySignage-Ekran-Kurulum.zip'
if (Test-Path $installerZip) {
    Remove-Item $installerZip -Force
}
Compress-Archive -Path (Join-Path $playerOut '*') -DestinationPath $installerZip -CompressionLevel Optimal

Write-Host "Paketler hazır: $OutputRoot"
