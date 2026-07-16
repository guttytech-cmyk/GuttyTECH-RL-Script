using System.Runtime.InteropServices;

namespace GuttyQuickChat;

/// <summary>Digita no chat do RL via keybd_event.</summary>
internal static class ChatSender
{
    private const byte VkShift = 0x10;
    private const byte VkControl = 0x11;
    private const byte VkMenu = 0x12;
    private const uint KeyeventfKeyup = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static void Send(string phrase, ushort openChatVk, int charDelayMs)
    {
        var hwnd = RocketLeagueWindow.Find();
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Janela do Rocket League nao encontrada.");

        if (!NativeInput.IsRocketLeagueFocused())
        {
            if (!RocketLeagueWindow.Focus(hwnd))
                AppMeta.Log("Aviso: nao conseguiu foco no RL.");
            Thread.Sleep(120);
        }

        ReleaseAllModifiers();

        TapKey((byte)openChatVk);
        AppMeta.Log($"Chat aberto (VK 0x{openChatVk:X2})");
        Thread.Sleep(450);

        TypeFast(phrase, charDelayMs);
        AppMeta.Log($"Texto digitado ({phrase.Length} chars)");
        Thread.Sleep(50);

        TapKey(0x0D);
        AppMeta.Log("Enter enviado");
    }

    private static void TypeFast(string text, int charDelayMs)
    {
        var layout = GetKeyboardLayout(0);
        foreach (var ch in text)
        {
            if (ch is '\r' or '\n')
                continue;
            TypeChar(ch, layout);
            if (charDelayMs > 0)
                Thread.Sleep(charDelayMs);
        }
    }

    private static void TypeChar(char ch, IntPtr layout)
    {
        var packed = VkKeyScanEx(ch, layout);
        if (packed == -1)
        {
            AppMeta.Log($"Tecla nao mapeada: '{ch}'");
            return;
        }

        var vk = (byte)(packed & 0xFF);
        var mods = (byte)((packed >> 8) & 0xFF);
        var scan = (byte)MapVirtualKey(vk, 0);

        if ((mods & 1) != 0) PressKey(VkShift);
        if ((mods & 2) != 0) PressKey(VkControl);
        if ((mods & 4) != 0) PressKey(VkMenu);

        PressAndRelease(vk, scan);

        if ((mods & 4) != 0) ReleaseKey(VkMenu);
        if ((mods & 2) != 0) ReleaseKey(VkControl);
        if ((mods & 1) != 0) ReleaseKey(VkShift);
    }

    private static void PressKey(byte vk) => keybd_event(vk, 0, 0, UIntPtr.Zero);
    private static void ReleaseKey(byte vk) => keybd_event(vk, 0, KeyeventfKeyup, UIntPtr.Zero);

    private static void PressAndRelease(byte vk, byte scan)
    {
        keybd_event(vk, scan, 0, UIntPtr.Zero);
        Thread.Sleep(1);
        keybd_event(vk, scan, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void TapKey(byte vk)
    {
        var scan = (byte)MapVirtualKey(vk, 0);
        keybd_event(vk, scan, 0, UIntPtr.Zero);
        Thread.Sleep(25);
        keybd_event(vk, scan, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static void ReleaseAllModifiers()
    {
        ReleaseKey(VkShift);
        ReleaseKey(VkControl);
        ReleaseKey(VkMenu);
        Thread.Sleep(15);
    }
}
