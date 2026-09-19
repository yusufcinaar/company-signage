#requires -version 5.1
param(
    [string]$InstallPath = 'C:\CompanySignagePublic\Server\Server',
    [int]$Port = 5000,
    [string]$SqlServer = 'localhost\SQLEXPRESS',
    [string]$DatabaseName = 'CompanySignagePublic',
    [string]$PublicHost = $env:COMPUTERNAME,
    [switch]$AllowUnencryptedLocalSql
)
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$') { throw 'Gecersiz veritabani adi.' }
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'PowerShell yönetici olarak açılmalıdır.' }
if ($SqlServer -eq 'localhost\SQLEXPRESS' -and -not (Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue)) { throw 'SQL Server Express bulunamadı.' }
if ($AllowUnencryptedLocalSql -and $SqlServer -notin @('localhost\SQLEXPRESS','.\SQLEXPRESS','(local)\SQLEXPRESS')) { throw 'Şifrelemesiz SQL yalnızca aynı bilgisayardaki SQLEXPRESS için kullanılabilir.' }
$isLocalSql = $SqlServer -in @('localhost\SQLEXPRESS','.\SQLEXPRESS','(local)\SQLEXPRESS','(localdb)\MSSQLLocalDB')
$encrypt = if ($isLocalSql -or $AllowUnencryptedLocalSql) { 'False' } else { 'True' }
$sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
if (-not $sqlcmd) { $sqlcmd = Get-ChildItem 'C:\Program Files\Microsoft SQL Server' -Recurse -Filter sqlcmd.exe -ErrorAction SilentlyContinue | Select-Object -First 1 }
if (-not $sqlcmd) { throw 'sqlcmd bulunamadı. SQL Server Command Line Utilities kurulmalıdır.' }
$sqlcmdPath = if ($sqlcmd.Source) { $sqlcmd.Source } else { $sqlcmd.FullName }
$setupSql = "IF SUSER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS; IF DB_ID(N'$DatabaseName') IS NULL CREATE DATABASE [$DatabaseName]; ALTER AUTHORIZATION ON DATABASE::[$DatabaseName] TO [NT AUTHORITY\SYSTEM];"
$sqlArgs = @('-S', $SqlServer, '-E', '-Q', $setupSql, '-b', '-l', '30')
if (-not $AllowUnencryptedLocalSql) { $sqlArgs += @('-N','-C') }
& $sqlcmdPath @sqlArgs
if ($LASTEXITCODE -ne 0) { throw 'MSSQL veritabanı veya servis hesabı yetkisi hazırlanamadı.' }
$source = $PSScriptRoot
New-Item -ItemType Directory -Force $InstallPath,'C:\CompanySignagePublic\Server\Uploads' | Out-Null
Copy-Item "$source\*" $InstallPath -Recurse -Force -Exclude 'install-server.ps1'
$configPath = Join-Path $InstallPath 'appsettings.json'
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$config.ConnectionStrings.DefaultConnection = "Server=$SqlServer;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=$encrypt"
$config.FileStorage.UploadFolder = 'C:\CompanySignagePublic\Server\Uploads'
$config.FileStorage.BaseUrl = "http://$PublicHost`:$Port"
$config.Kestrel.Endpoints.Http.Url = "http://0.0.0.0:$Port"
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
if (-not (Get-NetFirewallRule -DisplayName 'Company Signage Server' -ErrorAction SilentlyContinue)) { New-NetFirewallRule -DisplayName 'Company Signage Server' -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null }
$service = Get-Service CompanySignageServer -ErrorAction SilentlyContinue
if ($service) { Stop-Service CompanySignageServer -Force -ErrorAction SilentlyContinue; sc.exe delete CompanySignageServer | Out-Null; Start-Sleep -Seconds 2 }
sc.exe create CompanySignageServer binPath= "`"$InstallPath\CompanySignage.Web.exe`"" start= auto depend= "MSSQL`$SQLEXPRESS" DisplayName= "Company Signage Server" | Out-Null
sc.exe failure CompanySignageServer reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
Start-Service CompanySignageServer
Write-Host "Sunucu MSSQL ile kuruldu: http://$PublicHost`:$Port"
Write-Host "SQL: $SqlServer / $DatabaseName / Encrypt=$encrypt"
