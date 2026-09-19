#requires -version 2.0
param(
    [Parameter(Mandatory=$true)][string]$ServerUrl,
    [Parameter(Mandatory=$true)][string]$ScreenCode,
    [string]$DeviceToken = '',
    [string]$InstallPath = 'C:\CompanySignagePublic\Player\App'
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrEmpty($DeviceToken.Trim())) { throw 'Panelde belirlediginiz cihaz tokenini girin.' }

$rootPath = 'C:\CompanySignagePublic\Player'
$logPath = Join-Path $rootPath 'Logs\install.log'

function Write-InstallLog([string]$Message) {
    $line = '{0:yyyy-MM-dd HH:mm:ss} {1}' -f (Get-Date), $Message
    Write-Host $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
}

function Get-NetFramework48Release {
    try {
        $value = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction Stop
        return [int]$value.Release
    }
    catch { return 0 }
}

function Install-NetFramework48 {
    $downloadUrl = 'https://go.microsoft.com/fwlink/?linkid=2088631'
    $installerPath = Join-Path $env:TEMP 'NDP48-x86-x64-AllOS-ENU.exe'
    Write-InstallLog '.NET Framework 4.8 bulunamadı. Microsoft kurulum dosyası indiriliyor...'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]3072
    $client = New-Object Net.WebClient
    $client.DownloadFile($downloadUrl, $installerPath)
    Write-InstallLog '.NET Framework 4.8 kuruluyor. Bu işlem birkaç dakika sürebilir...'
    $process = Start-Process -FilePath $installerPath -ArgumentList '/q /norestart' -Wait -PassThru
    if (-not (@(0, 1641, 3010) -contains $process.ExitCode)) {
        throw ".NET Framework 4.8 kurulamadı (kod $($process.ExitCode)). Windows 7 kullanıyorsanız SP1, KB4490628 ve KB4474419 güncellemelerini kurup yeniden deneyin."
    }
    if (@(1641, 3010) -contains $process.ExitCode) {
        throw '.NET Framework 4.8 kuruldu. Bilgisayarı yeniden başlatın ve KURULUM-BASLAT.bat dosyasını tekrar çalıştırın.'
    }
}

try {
    New-Item -ItemType Directory -Force (Join-Path $rootPath 'Logs') | Out-Null
    Write-InstallLog 'Company Signage ekran kurulumu başlatıldı.'

    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'PowerShell yönetici olarak açılmalıdır.'
    }

    $osVersion = [Environment]::OSVersion.Version
    if ($osVersion -lt [Version]'6.1') { throw 'En az Windows 7 SP1 gerekir.' }
    if ($osVersion.Major -eq 6 -and $osVersion.Minor -eq 1) {
        $os = Get-WmiObject Win32_OperatingSystem
        if ([int]$os.ServicePackMajorVersion -lt 1) { throw 'Windows 7 Service Pack 1 (SP1) kurulmalıdır.' }
    }

    if ((Get-NetFramework48Release) -lt 528040) { Install-NetFramework48 }
    Write-InstallLog '.NET Framework 4.8 kontrolü başarılı.'

    Get-Process -Name 'CompanySignage.Player' -ErrorAction SilentlyContinue | Stop-Process -Force
    if (Get-Service CompanySignageAgent -ErrorAction SilentlyContinue) {
        Stop-Service CompanySignageAgent -Force
        sc.exe delete CompanySignageAgent | Out-Null
        Start-Sleep -Seconds 2
    }

    $source = Split-Path -Parent $MyInvocation.MyCommand.Path
    New-Item -ItemType Directory -Force $InstallPath,(Join-Path $rootPath 'Cache'),(Join-Path $rootPath 'Agent') | Out-Null
    Copy-Item "$source\*.exe","$source\*.dll","$source\*.config" $InstallPath -Force
    Copy-Item "$source\Agent\*" (Join-Path $rootPath 'Agent') -Force

    Add-Type -AssemblyName System.Web.Extensions
    $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $settingsObject = @{
        ServerUrl = $ServerUrl.TrimEnd('/')
        ScreenCode = $ScreenCode
        DeviceToken = $DeviceToken
        AutoStart = $true
        SoundEnabled = $false
    }
    $settings = $serializer.Serialize($settingsObject)
    $utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false
    [IO.File]::WriteAllText((Join-Path $rootPath 'settings.json'), $settings, $utf8NoBom)

    & icacls.exe $rootPath /grant '*S-1-5-32-545:(OI)(CI)M' /T /Q | Out-Null
    $playerExe = Join-Path $InstallPath 'CompanySignage.Player.exe'
    $quotedPlayerExe = '"' + $playerExe + '"'
    New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'CompanySignagePlayer' -Value $quotedPlayerExe -PropertyType String -Force | Out-Null

    # Player kullanıcı masaüstünde çalışır. Windows 7/10 oturum açılışında
    # görev ve Run kaydı ile başlatılır; tek örnek kilidi çift açılışı engeller.
    & schtasks.exe /Create /TN 'CompanySignagePlayer' /SC ONLOGON /TR $quotedPlayerExe /RL HIGHEST /IT /F | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-InstallLog 'Oturum açılış görevi oluşturuldu.'
    }
    else {
        Write-InstallLog 'Oturum açılış görevi oluşturulamadı; Windows Run kaydı kullanılacak.'
    }



    $agentExe = Join-Path $rootPath 'Agent\CompanySignage.Agent.Win7.exe'
    $quotedAgentExe = '"' + $agentExe + '"'
    sc.exe create CompanySignageAgent binPath= $quotedAgentExe start= auto DisplayName= "Company Signage Agent" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Agent servisi oluşturulamadı (kod $LASTEXITCODE)." }
    sc.exe failure CompanySignageAgent reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
    Start-Service CompanySignageAgent
    Write-InstallLog 'Company Signage Agent servisi başlatıldı.'

    try {
        Start-Process -FilePath $playerExe
    }
    catch {
        if ($_.Exception.NativeErrorCode -eq 14001) {
            throw 'Player Windows manifesti/bağımlılığı yüklenemedi (SxS 14001). En yeni kurulum paketini indirdiğinizden emin olun.'
        }
        throw
    }

    Write-InstallLog "Ekran kuruldu: $ScreenCode -> $ServerUrl"
    exit 0
}
catch {
    $message = $_.Exception.Message
    try { Write-InstallLog "HATA: $message" } catch { Write-Host "HATA: $message" }
    Write-Host ''
    Write-Host "Ayrıntılı kayıt: $logPath" -ForegroundColor Yellow
    exit 1
}
