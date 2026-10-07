<#
.SYNOPSIS
    Взаимодействие с окном запущенного билда игры: скриншот, клавиши, клики мышью.

.DESCRIPTION
    Использует Win32 (user32.dll/GDI) напрямую:
      * SetProcessDpiAwarenessContext — иначе координаты окна и CopyFromScreen расходятся на масштабировании;
      * EnumWindows/GetWindowRect — поиск окна нужного процесса и его границы;
      * CopyFromScreen — снимок области окна;
      * keybd_event/mouse_event — ввод (legacy Unity Input его видит).

.PARAMETER Action
    capture — сохранить PNG окна процесса;
    key     — отправить клавишу (VirtualKey, например 0x42 для 'B');
    click   — кликнуть в координатах окна (X, Y относительно клиентской области).

.EXAMPLE
    pwsh -File WindowTest.ps1 -ProcessName Client -Action capture -OutFile shot.png
    pwsh -File WindowTest.ps1 -ProcessName Client -Action key -VirtualKey 0x42
    pwsh -File WindowTest.ps1 -ProcessName Client -Action click -X 500 -Y 300
#>
param(
    [Parameter(Mandatory = $true)][string]$Action,
    [Parameter(Mandatory = $true)][string]$ProcessName,
    [string]$OutFile = "window.png",
    [int]$VirtualKey = 0,
    [int]$HoldMs = 1000,
    [int]$X = 0,
    [int]$Y = 0
)

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WinApi
{
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr param);
    public delegate bool EnumProc(IntPtr hwnd, IntPtr param);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

    public static IntPtr FindWindowForProcess(int pid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, param) =>
        {
            uint windowPid;
            GetWindowThreadProcessId(hwnd, out windowPid);
            if (windowPid != (uint)pid) return true;
            if (!IsWindowVisible(hwnd)) return true;

            RECT rect;
            if (!GetWindowRect(hwnd, out rect)) return true;
            if (rect.Right - rect.Left < 100 || rect.Bottom - rect.Top < 100) return true; // служебные окна

            found = hwnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    public static string TitleOf(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
'@

Add-Type -AssemblyName System.Drawing
[void][WinApi]::SetProcessDpiAwarenessContext([IntPtr](-4))   # PER_MONITOR_AWARE_V2

$process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $process) { throw "Процесс '$ProcessName' не найден." }

$hwnd = [WinApi]::FindWindowForProcess($process.Id)
if ($hwnd -eq [IntPtr]::Zero) { throw "У процесса '$ProcessName' (pid $($process.Id)) нет видимого окна." }

$rect = New-Object WinApi+RECT
[void][WinApi]::GetWindowRect($hwnd, [ref]$rect)
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
$title = [WinApi]::TitleOf($hwnd)

$client = New-Object WinApi+RECT
[void][WinApi]::GetClientRect($hwnd, [ref]$client)
$origin = New-Object WinApi+POINT
$origin.X = 0
$origin.Y = 0
[void][WinApi]::ClientToScreen($hwnd, [ref]$origin)

$clientWidth = $client.Right - $client.Left
$clientHeight = $client.Bottom - $client.Top

# Общий подъём окна: SetForegroundWindow может отказать, если вызывающий процесс
# не владеет фокусом, поэтому дополнительно поднимаем окно в Z-порядке.
function Show-GameWindow {
    param([IntPtr]$Handle)

    [void][WinApi]::ShowWindow($Handle, 9)                 # SW_RESTORE
    [void][WinApi]::BringWindowToTop($Handle)
    [void][WinApi]::SetWindowPos($Handle, [IntPtr](-1), 0, 0, 0, 0, 0x0003)   # HWND_TOPMOST, NOMOVE|NOSIZE
    [void][WinApi]::SetWindowPos($Handle, [IntPtr](-2), 0, 0, 0, 0, 0x0003)   # HWND_NOTOPMOST
    [void][WinApi]::SetForegroundWindow($Handle)
    Start-Sleep -Milliseconds 400
}

Write-Host "окно: '$title' hwnd=$hwnd pid=$($process.Id) rect=$($rect.Left),$($rect.Top) ${width}x${height}"
Write-Host "клиентская область: origin=$($origin.X),$($origin.Y) ${clientWidth}x${clientHeight} (рамка: x=$($origin.X - $rect.Left), y=$($origin.Y - $rect.Top))"

switch ($Action.ToLowerInvariant()) {
    'capture' {
        Show-GameWindow -Handle $hwnd

        $foreground = [WinApi]::GetForegroundWindow()
        Write-Host "активное окно совпадает с целевым: $($foreground -eq $hwnd)"

        $bitmap = New-Object System.Drawing.Bitmap $width, $height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        $bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()

        Write-Host "скриншот сохранён: $OutFile"
    }

    'capture_pw' {
        # Захват содержимого окна через PrintWindow: работает, даже если окно
        # перекрыто другим окном (CopyFromScreen в этом случае снимает чужое).
        Show-GameWindow -Handle $hwnd

        $bitmap = New-Object System.Drawing.Bitmap $width, $height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $hdc = $graphics.GetHdc()
        try {
            $ok = [WinApi]::PrintWindow($hwnd, $hdc, 2)     # PW_RENDERFULLCONTENT
        }
        finally {
            $graphics.ReleaseHdc($hdc)
        }
        $bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()

        Write-Host "PrintWindow=$ok, скриншот сохранён: $OutFile"
    }

    'key' {
        if ($VirtualKey -le 0) { throw "Для Action=key нужно указать -VirtualKey (например 0x42 для 'B')." }

        Show-GameWindow -Handle $hwnd
        [void][WinApi]::SetFocus($hwnd)

        [WinApi]::keybd_event([byte]$VirtualKey, 0, 0, [UIntPtr]::Zero)          # key down
        Start-Sleep -Milliseconds 60
        [WinApi]::keybd_event([byte]$VirtualKey, 0, 2, [UIntPtr]::Zero)          # key up
        Write-Host "клавиша 0x$('{0:X2}' -f $VirtualKey) отправлена в '$title'"
    }

    'hold' {
        if ($VirtualKey -le 0) { throw "Для Action=hold нужно указать -VirtualKey и -HoldMs." }

        Show-GameWindow -Handle $hwnd
        [void][WinApi]::SetFocus($hwnd)

        [WinApi]::keybd_event([byte]$VirtualKey, 0, 0, [UIntPtr]::Zero)          # key down
        Start-Sleep -Milliseconds $HoldMs
        [WinApi]::keybd_event([byte]$VirtualKey, 0, 2, [UIntPtr]::Zero)          # key up
        Write-Host "клавиша 0x$('{0:X2}' -f $VirtualKey) удерживалась $HoldMs мс в '$title'"
    }

    'click' {
        Show-GameWindow -Handle $hwnd

        [void][WinApi]::SetCursorPos($origin.X + $X, $origin.Y + $Y)
        Start-Sleep -Milliseconds 120
        [WinApi]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)   # LEFTDOWN
        Start-Sleep -Milliseconds 60
        [WinApi]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)   # LEFTUP
        Write-Host "клик в ($X,$Y) клиентской области '$title'"
    }

    'move' {
        [void][WinApi]::ShowWindow($hwnd, 9)
        [void][WinApi]::SetForegroundWindow($hwnd)
        Start-Sleep -Milliseconds 300

        [void][WinApi]::SetCursorPos($origin.X + $X, $origin.Y + $Y)
        Write-Host "курсор перемещён в ($X,$Y) клиентской области '$title'"
    }

    default { throw "Неизвестное действие '$Action' (capture|key|click)." }
}
