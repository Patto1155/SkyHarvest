# Launches Unity WITHOUT the elevated (admin) token, even when called from an
# elevated shell. Unity refuses to run cleanly as Administrator and pops a modal
# "running as admin" warning that blocks unattended -executeMethod runs.
#
# Trick: register a one-shot Scheduled Task with RunLevel=Limited, run it, wait
# for the spawned Unity process to exit, then clean up the task.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/run-unity-deelevated.ps1 `
#       -UnityArgs '-projectPath D:/APATPROJECTS/SkyHarvest -executeMethod PlayModeScreenshots.Run -logFile artifacts/playmode.log'
param(
    [Parameter(Mandatory = $true)][string]$UnityArgs,
    [string]$UnityExe = 'D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe'
)

$ErrorActionPreference = 'Stop'
$taskName = "SkyHarvestUnity_$([guid]::NewGuid().ToString('N').Substring(0,8))"

$action  = New-ScheduledTaskAction -Execute $UnityExe -Argument $UnityArgs
# Limited = run with the standard-user (non-elevated) token of the current user.
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -RunLevel Limited
$settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 1)

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
try {
    Start-ScheduledTask -TaskName $taskName
    Start-Sleep -Seconds 3

    # Wait until the task transitions back to Ready (Unity exited).
    while ((Get-ScheduledTask -TaskName $taskName).State -eq 'Running') {
        Start-Sleep -Seconds 2
    }
    $info = Get-ScheduledTaskInfo -TaskName $taskName
    Write-Output "Unity exit code: $($info.LastTaskResult)"
    exit $info.LastTaskResult
}
finally {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}
