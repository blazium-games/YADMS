$ErrorActionPreference = "Stop"

Write-Host "========================================"
Write-Host " Starting Build & Test Matrix (4 SKUs)"
Write-Host "========================================"

$skus = @(
    @{ Name = "Lite_Standard"; FFmpeg = "false"; Hack = "false" },
    @{ Name = "Lite_Hacked"; FFmpeg = "false"; Hack = "true" },
    @{ Name = "Bundled_Standard"; FFmpeg = "true"; Hack = "false" },
    @{ Name = "Bundled_Hacked"; FFmpeg = "true"; Hack = "true" }
)

foreach ($sku in $skus) {
    Write-Host "`n----------------------------------------"
    Write-Host " Building SKU: $($sku.Name)"
    Write-Host " - IncludeFfmpeg = $($sku.FFmpeg)"
    Write-Host " - IncludeGameHacking = $($sku.Hack)"
    Write-Host "----------------------------------------"
    
    # 1. Build main project
    $outPath = Join-Path $PWD "artifacts\$($sku.Name)"
    dotnet build ".\controller_mcp.csproj" -p:IncludeFfmpeg=$($sku.FFmpeg) -p:IncludeGameHacking=$($sku.Hack) -c Release -o $outPath
    
    # 2. Build test project
    dotnet build "..\controller_mcp.Tests\controller_mcp.Tests.csproj" -p:IncludeGameHacking=$($sku.Hack) -c Release
    
    # 3. Test execution
    Write-Host " Running Tests for $($sku.Name)..."
    dotnet test "..\controller_mcp.Tests\controller_mcp.Tests.csproj" -p:IncludeGameHacking=$($sku.Hack) -c Release --no-build
    
    Write-Host " SKU $($sku.Name) completed successfully!"
}

Write-Host "`n========================================"
Write-Host " All 4 SKUs Built and Tested Successfully!"
Write-Host "========================================"
