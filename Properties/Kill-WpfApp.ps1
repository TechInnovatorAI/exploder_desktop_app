# Kill Exploder.exe and apphost.exe if running

$processes = @("Exploder", "apphost")

foreach ($name in $processes) {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Killing process: $($_.Name) (PID: $($_.Id))"
        Stop-Process -Id $_.Id -Force
    }
}
Write-Host "✅ All target processes terminated."
