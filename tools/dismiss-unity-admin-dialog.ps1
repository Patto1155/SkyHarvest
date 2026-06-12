# Watches for Unity's "running as administrator" modal and auto-clicks
# "I wish to continue at my own risk" so unattended -executeMethod runs don't block.
#
# This machine's only account is "Administrator", so Unity ALWAYS shows this dialog
# (it checks group membership, not token elevation — de-elevation does not help).
# The button is custom-drawn (no UIAutomation InvokePattern), so we click its
# screen rectangle. SetProcessDPIAware() is required or the coords land off-target.
#
# Run in the background alongside a Unity launch:
#   Start a Unity -executeMethod run, then run this with -TimeoutSeconds covering startup.
param([int]$TimeoutSeconds = 120, [int]$PollMs = 1000)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class UnityDlg {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,int e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int c);
  const uint LD=0x0002, LU=0x0004;
  public static void Click(int x,int y){ SetCursorPos(x,y); System.Threading.Thread.Sleep(100); mouse_event(LD,0,0,0,0); System.Threading.Thread.Sleep(50); mouse_event(LU,0,0,0,0); }
}
'@
[UnityDlg]::SetProcessDPIAware() | Out-Null

$AE   = [System.Windows.Automation.AutomationElement]
$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, 'Unity is running as administrator.')
$btnC = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, 'I wish to continue at my own risk')

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$clicks = 0
while ((Get-Date) -lt $deadline) {
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
    if ($win) {
        $h = [IntPtr]$win.Current.NativeWindowHandle
        [UnityDlg]::ShowWindow($h, 9) | Out-Null
        [UnityDlg]::SetForegroundWindow($h) | Out-Null
        Start-Sleep -Milliseconds 300
        $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnC)
        if ($btn) {
            $r = $btn.Current.BoundingRectangle
            [UnityDlg]::Click([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
            $clicks++
            Write-Output "dismissed Unity admin dialog (#$clicks)"
            Start-Sleep -Seconds 2
        }
    }
    Start-Sleep -Milliseconds $PollMs
}
Write-Output "watcher done; total dismissals: $clicks"
