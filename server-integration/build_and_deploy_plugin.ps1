# build_and_deploy_plugin.ps1
# 这是一个辅助脚本，用于在本地自动创建 CS2 插件项目、添加依赖、移动源码、编译并生成 DLL

$ProjectName = "CenterHtmlMenu"

# =================【配置项】=================
# 如果您的 CS2 专用服务端不在 C:\steamcmd\cs2server，请将此处修改为您本机的实际路径
$ServerPath = "C:\steamcmd\cs2server" 
# ============================================

$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptPath

Write-Host "====== [1/4] Create .NET Class Library Project ======" -ForegroundColor Cyan
if (Test-Path $ProjectName) {
    Write-Host "Cleaning up old project folder..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $ProjectName -ErrorAction SilentlyContinue
}
dotnet new classlib -o $ProjectName

Write-Host "`n====== [2/4] Add CounterStrikeSharp Package ======" -ForegroundColor Cyan
Push-Location $ProjectName
dotnet add package CounterStrikeSharp.API
# 删除模板自带的 Class1.cs
if (Test-Path Class1.cs) {
    Remove-Item Class1.cs -Force
}
Pop-Location

Write-Host "`n====== [3/4] Copy Source Code File ======" -ForegroundColor Cyan
if (Test-Path "CenterHtmlMenu.cs") {
    Copy-Item "CenterHtmlMenu.cs" -Destination "$ProjectName/CenterHtmlMenu.cs" -Force
    Write-Host "Source code copied to project successfully." -ForegroundColor Green
} else {
    Write-Host "[ERROR] Could not find CenterHtmlMenu.cs in the current directory!" -ForegroundColor Red
    Exit
}

Write-Host "`n====== [4/4] Build Release Version ======" -ForegroundColor Cyan
Push-Location $ProjectName
dotnet build -c Release
Pop-Location

$DllSource = "$ProjectName/bin/Release/net8.0/$ProjectName.dll"

if (Test-Path $DllSource) {
    Write-Host "`n==========================================" -ForegroundColor Green
    Write-Host "[SUCCESS] Plugin build succeeded!" -ForegroundColor Green
    Write-Host "Compiled DLL: $DllSource" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Green
    
    # 尝试自动部署到您的本地 CS2 服务端
    if (Test-Path $ServerPath) {
        $PluginDestFolder = "$ServerPath\game\csgo\addons\counterstrikesharp\plugins\$ProjectName"
        if (!(Test-Path $PluginDestFolder)) {
            New-Item -ItemType Directory -Path $PluginDestFolder -Force | Out-Null
        }
        Copy-Item $DllSource -Destination "$PluginDestFolder/$ProjectName.dll" -Force
        Write-Host "`n[DEPLOY] Deployed successfully!" -ForegroundColor Green
        Write-Host "Installed to: $PluginDestFolder\$ProjectName.dll" -ForegroundColor Yellow
    } else {
        Write-Host "`n[WARN] Local CS2 directory not found at: $ServerPath" -ForegroundColor DarkYellow
        Write-Host "Please manually install the compiled plugin:" -ForegroundColor White
        Write-Host "1. Create folder: /game/csgo/addons/counterstrikesharp/plugins/$ProjectName/" -ForegroundColor White
        Write-Host "2. Copy $DllSource into that folder." -ForegroundColor White
    }
} else {
    Write-Host "`n[ERROR] Build failed. Please verify .NET SDK 8.0 installation and check code syntax." -ForegroundColor Red
}
