# ============================================================
# 塔罗牌卡面图片自托管下载脚本
# 将 78 张公有领域 Rider-Waite-Smith 卡面（WebP）下载到
# wwwroot/images/tarot/ 目录，实现本地化加载，避免依赖外部热链。
#
# 用法：在项目根目录执行
#   powershell -ExecutionPolicy Bypass -File scripts\download-tarot-images.ps1
# ============================================================

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Base = 'https://raw.githubusercontent.com/camik86/dsh-tarot-page/main/assets/cards'
$Root = Split-Path -Parent $PSScriptRoot
$Dest = Join-Path $Root 'wwwroot\images\tarot'

New-Item -ItemType Directory -Force -Path $Dest | Out-Null

$names = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -le 21; $i++) { $names.Add(('major-{0:00}.webp' -f $i)) }
foreach ($suit in @('wands', 'cups', 'swords', 'pentacles')) {
    for ($i = 1; $i -le 14; $i++) { $names.Add(('{0}-{1:00}.webp' -f $suit, $i)) }
}

$ok = 0
$fail = 0
Write-Host "开始下载 $($names.Count) 张塔罗牌图片 → $Dest"

foreach ($n in $names) {
    $url = "$Base/$n"
    $out = Join-Path $Dest $n
    try {
        Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing -TimeoutSec 30
        $ok++
    } catch {
        $fail++
        Write-Host "  ❌ 失败: $n"
    }
    if ((($ok + $fail) % 10) -eq 0) { Write-Host "  进度 $($ok + $fail)/$($names.Count)" }
}

Write-Host ""
Write-Host "完成：成功 $ok，失败 $fail"
if ($fail -eq 0) { Write-Host "✅ 全部图片已就绪，重新构建项目后即可本地加载卡面。" }
else { Write-Host "⚠️ 部分图片下载失败，可重新运行本脚本重试。" }
