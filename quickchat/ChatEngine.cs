using System.Runtime.InteropServices;

namespace GuttyQuickChat;

/// <summary>Atalhos via RegisterHotKey — nao conflita com keybd_event.</summary>
internal sealed class ChatEngine : IDisposable
{
    private const uint ModControl = 0x0002;
    private const uint ModNorepeat = 0x4000;
    private const int TeamIdOffset = 100;

    private readonly QuickChatConfig _config;
    private readonly Dictionary<int, HotKeyJob> _jobs = new();
    private readonly ushort _chatVk;
    private readonly ushort _teamChatVk;
    private readonly HiddenWindow _window;
    private readonly object _sendGate = new();

    private DateTime _lastSendUtc = DateTime.MinValue;
    private volatile bool _sending;

    private sealed record HotKeyJob(string Phrase, bool TeamChat);

    public ChatEngine(QuickChatConfig config)
    {
        _config = config;
        _chatVk = NativeInput.ResolveVk(config.Bindings.Chat);
        _teamChatVk = NativeInput.ResolveVk(config.Bindings.TeamChat);
        _window = new HiddenWindow(id => _ = TryHandleHotKey(id));

        RegisterAllHotKeys(config);
        AppMeta.Log($"Hotkeys OK: {_jobs.Count} | Geral={config.Bindings.Chat} Time={config.Bindings.TeamChat}");
    }

    public bool TryHandleHotKey(int hotKeyId)
    {
        if (!_jobs.TryGetValue(hotKeyId, out var job))
            return false;

        if (_config.RequireRocketLeagueFocus && !NativeInput.IsRocketLeagueFocused())
        {
            AppMeta.Log($"Hotkey {hotKeyId} ignorada: RL sem foco.");
            return true;
        }

        lock (_sendGate)
        {
            if (_sending)
                return true;

            var elapsed = (DateTime.UtcNow - _lastSendUtc).TotalMilliseconds;
            if (elapsed < _config.SendCooldownMs)
                return true;

            _sending = true;
        }

        var phrase = job.Phrase;
        var team = job.TeamChat;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                SendPhrase(phrase, team);
            }
            finally
            {
                lock (_sendGate)
                {
                    _sending = false;
                    _lastSendUtc = DateTime.UtcNow;
                }
            }
        });

        return true;
    }

    public void Dispose()
    {
        foreach (var id in _jobs.Keys.ToList())
            NativeInput.UnregisterHotKey(_window.Handle, id);
        _jobs.Clear();
        _window.Dispose();
    }

    private void RegisterAllHotKeys(QuickChatConfig config)
    {
        var id = 1;
        foreach (var (keyName, phrase) in config.DirectBinds.OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            ushort vk;
            try
            {
                vk = NativeInput.ResolveVk(keyName);
            }
            catch (Exception ex)
            {
                AppMeta.Log($"Hotkey ignorada ({keyName}): {ex.Message}");
                continue;
            }

            var mods = ModNorepeat;
            if (NativeInput.RegisterHotKey(_window.Handle, id, mods, vk))
                _jobs[id] = new HotKeyJob(phrase, TeamChat: false);
            else
                AppMeta.Log($"Falha tecla {keyName} (geral): {Marshal.GetLastWin32Error()}");

            var teamId = id + TeamIdOffset;
            if (NativeInput.RegisterHotKey(_window.Handle, teamId, mods | ModControl, vk))
                _jobs[teamId] = new HotKeyJob(phrase, TeamChat: true);
            else
                AppMeta.Log($"Falha Ctrl+{keyName} (time): {Marshal.GetLastWin32Error()}");

            id++;
        }

        if (_jobs.Count == 0)
            throw new InvalidOperationException("Nenhuma hotkey registrada. Feche outras instancias do QuickChat.");
    }

    private void SendPhrase(string phrase, bool teamChat)
    {
        if (RocketLeagueWindow.Find() == IntPtr.Zero)
        {
            AppMeta.Log("Envio cancelado: Rocket League nao encontrado.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [X] Rocket League nao encontrado.");
            Console.ResetColor();
            return;
        }

        try
        {
            var openVk = teamChat ? _teamChatVk : _chatVk;
            var channel = teamChat
                ? $"time ({char.ToUpperInvariant(_config.Bindings.TeamChat[0])})"
                : $"geral ({char.ToUpperInvariant(_config.Bindings.Chat[0])})";
            ChatSender.Send(phrase, openVk, _config.TypingDelayMs);
            AppMeta.Log($"Enviado [{channel}]: {phrase}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  >> [{channel}] {phrase}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [+] OK");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            AppMeta.Log($"Falha ao enviar: {ex.Message}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [X] {ex.Message}");
            Console.ResetColor();
        }
    }

    public void SendTestPhrase()
    {
        var phrase = _config.DirectBinds.GetValueOrDefault("D2") ?? "to indo meu mano";
        Console.WriteLine($"  >> TESTE: \"{phrase}\" em 2s — clique no RL!");
        Thread.Sleep(2000);
        SendPhrase(phrase, teamChat: false);
    }
}
