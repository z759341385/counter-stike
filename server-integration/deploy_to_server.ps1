# deploy_to_server.ps1
# 这是一个用于将本地 C# 源码自动上传至远程服务器并进行云编译、部署与重启服务的脚本

$SSH_HOST = "111.35.165.130"
$SSH_PORT = "42553"
$SSH_USER = "root"
$REMOTE_PROJECT_DIR = "/www/steam/CS2HextechPlugin"
$REMOTE_PLUGIN_DIR = "/www/steam/cs2server/game/csgo/addons/counterstrikesharp/plugins/CS2HextechPlugin"

$ScriptPath = $PSScriptRoot
if ([string]::IsNullOrEmpty($ScriptPath)) { $ScriptPath = $PWD.Path }
Set-Location $ScriptPath
Write-Host "ScriptPath is: $ScriptPath"

Write-Host "====== [1/4] Preparing Source Code ======" -ForegroundColor Cyan
$SourceFile = "CS2HextechPlugin.cs"
if (!(Test-Path $SourceFile)) {
    Write-Host "[ERROR] $SourceFile not found in current directory!" -ForegroundColor Red
    Exit
}

Write-Host "Formatting code encoding to UTF-8 with BOM..." -ForegroundColor Gray
$AbsoluteSourcePath = Join-Path $ScriptPath $SourceFile
$SourceContent = [System.IO.File]::ReadAllText($AbsoluteSourcePath, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($AbsoluteSourcePath, $SourceContent, [System.Text.Encoding]::UTF8)

$bytes = [System.IO.File]::ReadAllBytes($AbsoluteSourcePath)
$b64 = [Convert]::ToBase64String($bytes)
$TempB64File = "CS2HextechPlugin.cs.b64"
$AbsoluteB64Path = Join-Path $ScriptPath $TempB64File
[System.IO.File]::WriteAllText($AbsoluteB64Path, $b64, [System.Text.Encoding]::ASCII)

Write-Host "`n====== [2/4] Uploading Code to Server ======" -ForegroundColor Cyan
cmd /c "type `"$AbsoluteB64Path`" | ssh -p $SSH_PORT $SSH_USER@$SSH_HOST ""base64 -d > $REMOTE_PROJECT_DIR/$SourceFile"""
Remove-Item $AbsoluteB64Path -Force

Write-Host "`n====== [3/4] Compiling on Remote Server (.NET 10) ======" -ForegroundColor Cyan
$BuildCommand = "export PATH=/root/.dotnet:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin && cd $REMOTE_PROJECT_DIR && dotnet build -c Release"
ssh -p $SSH_PORT "$SSH_USER@$SSH_HOST" $BuildCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Remote build failed! Please check compile errors above." -ForegroundColor Red
    Exit
}

Write-Host "`n====== [4/4] Deploying DLL and Restarting Server ======" -ForegroundColor Cyan
$DeployCommand = "export PATH=/root/.dotnet:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin && cp $REMOTE_PROJECT_DIR/bin/Release/net10.0/CS2HextechPlugin.dll $REMOTE_PLUGIN_DIR/ && pkill -9 cs2; sleep 1; screen -L -Logfile /tmp/screen_cs2.log -dmS cs2 /www/steam/cs2server/start_cs2.sh"
ssh -p $SSH_PORT "$SSH_USER@$SSH_HOST" $DeployCommand

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "[SUCCESS] Plugin successfully compiled and deployed!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green

Write-Host "`n====== Checking CS2 Screen status ======" -ForegroundColor Cyan
ssh -p $SSH_PORT "$SSH_USER@$SSH_HOST" "screen -ls"

Write-Host "`n====== Checking CounterStrikeSharp Loading Logs ======" -ForegroundColor Cyan
Start-Sleep -Seconds 3
ssh -p $SSH_PORT "$SSH_USER@$SSH_HOST" "tail -n 15 /www/steam/cs2server/game/csgo/addons/counterstrikesharp/logs/log-cssharp*.txt"
