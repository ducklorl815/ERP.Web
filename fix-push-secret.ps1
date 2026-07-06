# 修正 Git 歷史中的 OpenAI API Key（Push Protection 阻擋用）
# 用法：在 PowerShell 於專案根目錄執行 .\fix-push-secret.ps1

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$git = Get-Command git -ErrorAction SilentlyContinue
if (-not $git) {
    Write-Host "找不到 git，請在已安裝 Git 的終端機執行此腳本。" -ForegroundColor Red
    exit 1
}

$branch = git branch --show-current
if ($branch -ne "F_英聽") {
    Write-Host "目前分支為 '$branch'，請先切換：git checkout F_英聽" -ForegroundColor Yellow
    exit 1
}

$badCommit = "c57e7e92e27bf66e8eaf920ea975983c3aa7cb17"
$baseCommit = "25dc05a8890cf5b52700f695cfd10d078ed3f41b"

if (-not (git cat-file -e "${badCommit}^{commit}" 2>$null)) {
    Write-Host "找不到問題 commit，可能已修正過。" -ForegroundColor Yellow
    exit 0
}

if ((Get-Content "ERP.Web\appsettings.Development.json" -Raw) -match '"OpenAiApiKey"\s*:\s*"sk-') {
    Write-Host "appsettings.Development.json 仍含 API Key，請先清空 OpenAiApiKey。" -ForegroundColor Red
    exit 1
}

Write-Host "將合併 c57e7e92 與後續 commit，重寫為不含 Key 的單一 commit..." -ForegroundColor Cyan
git reset --soft $baseCommit
git add -A
git commit -m @"
註冊 OpenAI TTS（英聽／中聽）

- 新增 OpenAiExamTtsService
- API Key 改由 User Secrets 管理，不寫入版控
"@

Write-Host ""
Write-Host "完成。請執行：" -ForegroundColor Green
Write-Host "  git push -v origin F_英聽:F_英聽" -ForegroundColor White
