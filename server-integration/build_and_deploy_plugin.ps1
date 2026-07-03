# build_and_deploy_plugin.ps1
# 这是一个辅助脚本，用于在本地自动创建 CS2 插件项目、添加依赖、移动源码、编译并生成 DLL

$ProjectName = "CenterHtmlMenu"

# =================【配置项】=================
# 如果您的 CS2 专用服务端不在 C:\steamcmd\cs2server，请将此处修改为您本机的实际路径
$ServerPath = "C:\steamcmd\cs2server" 
# ============================================

$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptPath

Write-Host "====== [1/4] 创建 .NET 类库项目 ======" -ForegroundColor Cyan
if (Test-Path $ProjectName) {
    Write-Host "发现已有项目目录，正在清理旧文件..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $ProjectName -ErrorAction SilentlyContinue
}
dotnet new classlib -o $ProjectName

Write-Host "`n====== [2/4] 添加 CounterStrikeSharp 依赖 ======" -ForegroundColor Cyan
Push-Location $ProjectName
dotnet add package CounterStrikeSharp.API
# 删除模板自带的 Class1.cs
if (Test-Path Class1.cs) {
    Remove-Item Class1.cs -Force
}
Pop-Location

Write-Host "`n====== [3/4] 拷贝源码文件 ======" -ForegroundColor Cyan
if (Test-Path "CenterHtmlMenu.cs") {
    Copy-Item "CenterHtmlMenu.cs" -Destination "$ProjectName/CenterHtmlMenu.cs" -Force
    Write-Host "源码 CenterHtmlMenu.cs 已成功拷贝至项目文件夹内。" -ForegroundColor Green
} else {
    Write-Host "❌ 未能在当前目录下找到 CenterHtmlMenu.cs 源码文件！" -ForegroundColor Red
    Exit
}

Write-Host "`n====== [4/4] 编译 Release 版本 ======" -ForegroundColor Cyan
Push-Location $ProjectName
dotnet build -c Release
Pop-Location

$DllSource = "$ProjectName/bin/Release/net8.0/$ProjectName.dll"

if (Test-Path $DllSource) {
    Write-Host "`n==========================================" -ForegroundColor Green
    Write-Host "✅ 插件编译成功！" -ForegroundColor Green
    Write-Host "编译生成的 DLL 路径: $DllSource" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Green
    
    # 尝试自动部署到您的本地 CS2 服务端
    if (Test-Path $ServerPath) {
        $PluginDestFolder = "$ServerPath\game\csgo\addons\counterstrikesharp\plugins\$ProjectName"
        if (!(Test-Path $PluginDestFolder)) {
            New-Item -ItemType Directory -Path $PluginDestFolder -Force | Out-Null
        }
        Copy-Item $DllSource -Destination "$PluginDestFolder/$ProjectName.dll" -Force
        Write-Host "`n🚀 自动部署成功！" -ForegroundColor Green
        Write-Host "插件已安装至: $PluginDestFolder\$ProjectName.dll" -ForegroundColor Yellow
    } else {
        Write-Host "`n⚠️ 提示：未检测到本地 CS2 服务端路径：$ServerPath" -ForegroundColor DarkYellow
        Write-Host "由于未找到服务端目录，请手动完成以下两步安装：" -ForegroundColor White
        Write-Host "1. 在服务器目录下创建新文件夹：/game/csgo/addons/counterstrikesharp/plugins/$ProjectName/" -ForegroundColor White
        Write-Host "2. 将编译好的 $DllSource 复制到该文件夹下。" -ForegroundColor White
    }
} else {
    Write-Host "`n❌ 编译失败，请检查是否安装了 .NET SDK 8.0，或者代码中是否存在语法错误。" -ForegroundColor Red
}
