# 脚本功能：批量转换项目中所有 .cs 脚本为 UTF-8 with BOM 编码
# 使用方法：
# 1. 将此脚本放在 Unity 项目根目录（与 Assets 同级）
# 2. 右键脚本 → 使用 PowerShell 运行
# 3. 等待执行完成后，在 Unity 中刷新工程

# 备份警告
Write-Host "===== Unity C# 脚本批量转码工具 =====" -ForegroundColor Cyan
Write-Host "⚠️  警告：请先使用 git 提交或备份整个项目！" -ForegroundColor Red
$confirm = Read-Host "是否继续？(输入 y 确认)"

if ($confirm -ne "y") {
    Write-Host "已取消" -ForegroundColor Yellow
    exit
}

# 设置编码提供者
$encodingUtf8 = New-Object System.Text.UTF8Encoding $true  # $true = 带 BOM
$encodingSource = [System.Text.Encoding]::GetEncoding(936)   # 936 = GBK

# 查找所有 C# 文件
$csFiles = Get-ChildItem -Path ".\Assets" -Filter "*.cs" -Recurse -File

Write-Host "找到 $($csFiles.Count) 个 .cs 文件" -ForegroundColor Green

$successCount = 0
$failCount = 0

foreach ($file in $csFiles) {
    try {
        # 先读取字节流，避免文本解析错误
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        
        # 尝试检测是否已是 UTF-8 with BOM
        $isUtf8WithBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        
        if (-not $isUtf8WithBom) {
            # 按 GBK 解码文本（源文件应该是 GBK 编码）
            $text = $encodingSource.GetString($bytes)
            
            # 用 UTF-8 with BOM 写回
            [System.IO.File]::WriteAllText($file.FullName, $text, $encodingUtf8)
            
            Write-Host "✓ $($file.Name)" -ForegroundColor Gray
            $successCount++
        }
        else {
            Write-Host "Already UTF-8 BOM: $($file.Name)" -ForegroundColor DarkGray
        }
    }
    catch {
        Write-Host "✗ 失败: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "===== 完成 =====" -ForegroundColor Cyan
Write-Host "成功: $successCount, 失败: $failCount" -ForegroundColor Green
Write-Host "请回到 Unity 刷新工程"
