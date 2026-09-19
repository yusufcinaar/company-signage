param(
    [string]$Python = 'python',
    [string]$SqlServer = 'localhost\SQLEXPRESS'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$taskRoot = Join-Path $env:TEMP ('CompanySignageTest-' + [guid]::NewGuid().ToString('N'))
$taskServer = Join-Path $taskRoot 'server'
$taskDatabase = 'CompanySignageTest_' + [guid]::NewGuid().ToString('N')
$taskProcess = $null
$saved = @{}
$envNames = @('ConnectionStrings__DefaultConnection','FileStorage__UploadFolder','FileStorage__BaseUrl','Kestrel__Endpoints__Http__Url','ASPNETCORE_ENVIRONMENT','SIGNAGE_TEST_URL','SIGNAGE_TEST_PASSWORD','SIGNAGE_TEST_STATE')
foreach ($name in $envNames) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    & dotnet build (Join-Path $repo 'CompanySignage.sln') -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed' }
    & dotnet build (Join-Path $PSScriptRoot 'PlayerRegression\PlayerRegression.csproj') -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Regression build failed' }
    New-Item -ItemType Directory -Path $taskServer | Out-Null
    Copy-Item -Path (Join-Path $repo 'src\CompanySignage.Web\bin\Release\net10.0\*') -Destination $taskServer -Recurse
    Copy-Item -LiteralPath (Join-Path $repo 'src\CompanySignage.Web\wwwroot') -Destination $taskServer -Recurse
    New-Item -ItemType Directory -Path (Join-Path $taskServer 'Data') -Force | Out-Null
    '{"AutoBackupEnabled":false}' | Set-Content (Join-Path $taskServer 'Data\backup-settings.json')
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
    $env:ConnectionStrings__DefaultConnection = "Server=$SqlServer;Database=$taskDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
    $env:FileStorage__UploadFolder = Join-Path $taskRoot 'uploads'
    $env:FileStorage__BaseUrl = "http://127.0.0.1:$port"
    $env:Kestrel__Endpoints__Http__Url = $env:FileStorage__BaseUrl
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:SIGNAGE_TEST_URL = $env:FileStorage__BaseUrl
    $env:SIGNAGE_TEST_PASSWORD = 'Admin123!'
    $env:SIGNAGE_TEST_STATE = Join-Path $taskRoot 'test-state.json'
    $taskProcess = Start-Process -FilePath 'dotnet' -ArgumentList 'CompanySignage.Web.dll' -WorkingDirectory $taskServer -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskRoot 'server.log') -RedirectStandardError (Join-Path $taskRoot 'server-error.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        if ($taskProcess.HasExited) { throw 'Test server exited; inspect logs' }
        try {
            $null = Invoke-WebRequest ($env:SIGNAGE_TEST_URL + '/Auth/Login') -UseBasicParsing -TimeoutSec 2
            $ready = $true; break
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'Test server startup timed out' }
    & $Python (Join-Path $PSScriptRoot 'integration.py')
    if ($LASTEXITCODE -ne 0) { throw 'HTTP regression failed' }
    & (Join-Path $PSScriptRoot 'PlayerRegression\bin\Release\net48\PlayerRegression.exe') $env:SIGNAGE_TEST_STATE (Join-Path $taskRoot 'player')
    if ($LASTEXITCODE -ne 0) { throw 'Player regression failed' }
    Write-Host 'All integration checks passed.'
}
finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force }
    if ($taskDatabase -notmatch '^CompanySignageTest_[a-f0-9]{32}$') { throw 'Unexpected test database name' }
    $connection = New-Object System.Data.SqlClient.SqlConnection "Server=$SqlServer;Database=master;Integrated Security=True;TrustServerCertificate=True"
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = "IF DB_ID(N'$taskDatabase') IS NOT NULL BEGIN ALTER DATABASE [$taskDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$taskDatabase]; END"
        $null = $command.ExecuteNonQuery()
    } finally { $connection.Dispose() }
    foreach ($name in $envNames) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    Write-Host "Test logs: $taskRoot"
}
