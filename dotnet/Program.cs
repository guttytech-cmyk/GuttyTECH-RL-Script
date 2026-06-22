using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace GuttyRL;

internal static class Program
{
    private const string Version = "v22.0";

    private static readonly string GuttyDir =
        Path.Combine(
            Environment.GetEnvironmentVariable("GUTTYRL_HOME") is { Length: > 0 } home
                ? home
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "GuttyTECH", "RL-Optimizer-v22");
    private static string BackupDir => Path.Combine(GuttyDir, "Backups");
    private static string OrigBackup => Path.Combine(BackupDir, "TASystemSettings.original.ini");
    private static string LogFile => Path.Combine(GuttyDir, "log.txt");

    private static readonly string[] DisplayKeys =
        { "ResX", "ResY", "Fullscreen", "Borderless", "AutoDetectDesktopResolution" };

    private static string? _cfg;

    private static int Main(string[] args)
    {
        try { Console.Title = "GUTTYTECH - RL INI OPTIMIZER " + Version; } catch { }
        bool ansi = Vt.Enable();
        Ui.Init(ansi);
        try { Directory.CreateDirectory(BackupDir); } catch { }

        _cfg = FindIni();
        if (_cfg is null)
        {
            Ui.Cls();
            Ui.Banner(false);
            Ui.PanelTop("ERRO");
            Ui.PanelLine(Ui.C("X  TASystemSettings.ini nao encontrado.", Ui.Red));
            Ui.PanelLine(Ui.C("Abra o Rocket League uma vez e rode de novo.", Ui.Gray));
            Ui.PanelBottom();
            Ui.EnterButton();
            return 1;
        }

        // Modo nao-interativo (e relancamento elevado): GuttyRL.exe COMPLETO [/keepopen]
        if (args.Length > 0)
        {
            string mode = args[0].Trim('/', '-').ToUpperInvariant();
            mode = mode switch { "1" => "COMPLETO", "2" => "CRIADOR", "3" => "REMOVER", _ => mode };
            bool keepOpen = args.Length > 1 && args[1].Equals("/keepopen", StringComparison.OrdinalIgnoreCase);
            int rc = Dispatch(mode, keepOpen);
            if (keepOpen) Ui.EnterButton();
            return rc;
        }

        Ui.Intro();
        while (true)
        {
            ShowMenu();
            switch (Console.ReadLine()?.Trim())
            {
                case "1": Dispatch("COMPLETO", true); Ui.EnterButton(); break;
                case "2": Dispatch("CRIADOR", true); Ui.EnterButton(); break;
                case "3": Dispatch("REMOVER", true); Ui.EnterButton(); break;
                case "4": Ui.ShowCursor(); Goodbye(); return 0;
            }
        }
    }

    private static int Dispatch(string mode, bool interactive)
    {
        if (mode == "REMOVER") return Remover(interactive);
        if (mode is "COMPLETO" or "CRIADOR") return Apply(mode, interactive);
        Ui.SectionTitle("ARGUMENTO INVALIDO", Ui.Amber);
        Console.WriteLine(Ui.C("  Use: GuttyRL.exe [COMPLETO | CRIADOR | REMOVER]", Ui.Gray));
        return 2;
    }

    // -------------------------------------------------------------- Menu
    private static void ShowMenu()
    {
        Ui.HideCursor();
        Ui.Cls();
        Ui.Banner(false);

        var (label, locked, cat) = ReadState();
        var dot = cat == 2 ? Ui.Green : cat == 1 ? Ui.Amber : Ui.Gray;

        Ui.PanelTop("ALVO");
        Ui.PanelLine(Ui.Field("Arquivo", Ui.C(FitPath(_cfg!, 58), Ui.Gray)));
        Ui.PanelLine(Ui.Field("Estado", Ui.Dot(dot, label)));
        string trava = locked ? Ui.Dot(Ui.Green, "SIM") : Ui.Dot(Ui.Amber, "NAO");
        string adm = IsAdmin() ? Ui.Dot(Ui.Green, "SIM") : Ui.Dot(Ui.Gray, "nao necessario");
        Ui.PanelLine(Ui.Field("Protegido", trava + "    " + Ui.C("Admin ", Ui.DimC) + adm));
        Ui.PanelBottom();
        Ui.Gap();

        Ui.PanelTop("MODOS");
        Ui.PanelLine(Card("1", "COMPLETO", "FPS maximo - graficos minimos", Ui.Red));
        Ui.PanelLine(Card("2", "CRIADOR", "Otimizado - visual preservado", Ui.Cyan));
        Ui.PanelLine(Card("3", "REMOVER", "Restaurar original / stock", Ui.Amber));
        Ui.PanelLine(Card("4", "SAIR", "fechar o GuttyRL", Ui.DimC));
        Ui.PanelBottom();
        Ui.Prompt("Escolha (1-4)");
    }

    private static string Card(string n, string title, string desc, (int r, int g, int b) c)
        => Ui.C("[" + n + "]", c) + " " + Ui.C("▌", c) + " " + Ui.B(title.PadRight(9), Ui.White) + "  " + Ui.C(desc, Ui.DimC);

    private static string FitPath(string p, int max)
        => p.Length <= max ? p : "..." + p[^(max - 3)..];

    // -------------------------------------------------------------- Apply
    private static int Apply(string mode, bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        var acc = Ui.ModeColor(mode);
        if (interactive) { Ui.Cls(); Ui.MiniBannerIfTall(acc); Ui.TitleBar("APLICANDO MODO " + mode, acc); }
        if (!WriteTest()) return 1;

        EnsureOriginalBackup();
        string dsrc = File.Exists(OrigBackup) ? OrigBackup : _cfg!;
        var disp = ReadDisplay(dsrc);
        string template = mode == "COMPLETO" ? Templates.Completo : Templates.Criador;
        string content = ApplyDisplay(template, disp);

        if (interactive)
        {
            Ui.StepAnimated("Backup de seguranca", () => { Backup(); return true; });
            Ui.StepAnimated("Destravando o arquivo", () => { Unlock(_cfg!); return true; });
            if (!Ui.StepAnimated("Gravando otimizacao", () => DoWrite(content, mode))) return FailOrElevate(mode, interactive);
            Ui.StepAnimated("Protegendo (somente-leitura)", () => { try { File.SetAttributes(_cfg!, FileAttributes.ReadOnly); } catch { } return true; });
        }
        else
        {
            Backup();
            Unlock(_cfg!);
            if (!DoWrite(content, mode)) return FailOrElevate(mode, interactive);
            try { File.SetAttributes(_cfg!, FileAttributes.ReadOnly); } catch { }
        }

        Log($"Aplicado {mode}.");
        if (interactive) Ui.CompletionSuccess(mode, acc, BackupDir, ChecklistFor(mode));
        return 0;
    }

    private static bool DoWrite(string content, string mode)
    {
        try
        {
            if (File.Exists(_cfg!)) File.Delete(_cfg!);
            File.WriteAllText(_cfg!, content, new UTF8Encoding(false));
            return File.ReadAllText(_cfg!).Contains("GUTTYTECH-RL-OPTIMIZER=" + mode);
        }
        catch { return false; }
    }

    private static (string label, string value)[] ChecklistFor(string mode) => mode == "COMPLETO"
        ? new[] { ("Render", "Performance"), ("Textura", "Performance"), ("Anti-Alias", "Desligado"), ("V-Sync", "Desligado"), ("Efeitos", "Sombras/Luz/Clima OFF") }
        : new[] { ("Render", "Alta Qualidade"), ("Textura", "Alta Qualidade"), ("Sombras", "Dinamicas OFF"), ("Efeitos", "MotionBlur/DoF/Bloom OFF"), ("V-Sync", "Desligado") };

    // -------------------------------------------------------------- Remover
    private static int Remover(bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        if (interactive) { Ui.Cls(); Ui.MiniBannerIfTall(Ui.MAmber); Ui.TitleBar("REMOVENDO / RESTAURANDO", Ui.MAmber); }
        if (!WriteTest()) return 1;

        bool fromOriginal = File.Exists(OrigBackup);
        bool Restore()
        {
            try
            {
                if (fromOriginal)
                {
                    if (File.Exists(_cfg!)) File.Delete(_cfg!);
                    File.Copy(OrigBackup, _cfg!, true);
                }
                else
                {
                    var disp = ReadDisplay(_cfg!);
                    string content = ApplyDisplay(Templates.Stock, disp);
                    if (File.Exists(_cfg!)) File.Delete(_cfg!);
                    File.WriteAllText(_cfg!, content, new UTF8Encoding(false));
                }
                try { File.SetAttributes(_cfg!, FileAttributes.Normal); } catch { }
                return true;
            }
            catch { return false; }
        }

        if (interactive)
        {
            Ui.StepAnimated("Destravando o arquivo", () => { Unlock(_cfg!); return true; });
            Ui.StepAnimated("Backup de seguranca", () => { Backup(); return true; });
            if (!Ui.StepAnimated(fromOriginal ? "Restaurando seu original" : "Restaurando padrao de fabrica", Restore))
                return FailOrElevate("REMOVER", interactive);
            Log("REMOVER concluido.");
            Ui.CompletionMessage(Ui.MAmber, "RESTAURADO", new[]
            {
                "Configuracao restaurada e arquivo DESTRAVADO.",
                "Sua resolucao foi mantida; o jogo volta a gerenciar o arquivo."
            });
        }
        else
        {
            Unlock(_cfg!);
            Backup();
            if (!Restore()) return FailOrElevate("REMOVER", interactive);
            Log("REMOVER concluido.");
        }
        return 0;
    }

    // -------------------------------------------------------------- Find INI
    private static string? FindIni()
    {
        string? ov = Environment.GetEnvironmentVariable("GUTTYRL_INI");
        if (!string.IsNullOrEmpty(ov) && SafeExists(ov)) return ov;

        const string rel = @"My Games\Rocket League\TAGame\Config\TASystemSettings.ini";
        var tried = new List<string>();
        void Add(string p) { if (!string.IsNullOrEmpty(p)) tried.Add(p); }

        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), rel));
        string up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Add(Path.Combine(up, "Documents", rel));
        Add(Path.Combine(up, "OneDrive", "Documents", rel));
        Add(Path.Combine(up, "OneDrive - Personal", "Documents", rel));
        Add(Path.Combine(up, "OneDrive - Pessoal", "Documents", rel));

        foreach (var p in tried) if (SafeExists(p)) return p;

        try
        {
            string usersRoot = Path.GetDirectoryName(up)!;
            foreach (var u in Directory.GetDirectories(usersRoot))
                foreach (var sub in new[] { "Documents", @"OneDrive\Documents", @"OneDrive - Personal\Documents" })
                {
                    string p = Path.Combine(u, sub, rel);
                    if (SafeExists(p)) return p;
                }
        }
        catch { }
        return null;
    }

    private static bool SafeExists(string p) { try { return File.Exists(p); } catch { return false; } }

    // -------------------------------------------------------------- State
    private static (string label, bool locked, int cat) ReadState()
    {
        string label = "Original / padrao (nao otimizado)";
        int cat = 0;
        try
        {
            string text = File.ReadAllText(_cfg!);
            if (text.Contains("GUTTYTECH-RL-OPTIMIZER=COMPLETO")) { label = "COMPLETO aplicado (FPS maximo)"; cat = 2; }
            else if (text.Contains("GUTTYTECH-RL-OPTIMIZER=CRIADOR")) { label = "CRIADOR aplicado (visual + perf)"; cat = 2; }
            else if (text.Contains("MaxLODSize=16")) { label = "Otimizado por versao antiga v21"; cat = 1; }
        }
        catch { }
        bool locked = false;
        try { locked = (File.GetAttributes(_cfg!) & FileAttributes.ReadOnly) != 0; } catch { }
        return (label, locked, cat);
    }

    private static bool IsAdmin()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    // -------------------------------------------------------------- Helpers
    private static bool CheckGame(bool interactive)
    {
        if (Environment.GetEnvironmentVariable("GUTTYRL_SKIP_GAMECHECK") == "1") return true;
        if (GetRl().Length == 0) return true;
        Ui.SectionTitle("ROCKET LEAGUE ABERTO", Ui.Amber);
        Console.WriteLine(Ui.C("  O jogo sobrescreve o arquivo ao fechar. Feche-o antes de aplicar.", Ui.Gray));
        if (!interactive) { Console.WriteLine(Ui.C("  Feche o jogo e rode de novo.", Ui.Red)); return false; }
        Ui.Prompt("Fechar o jogo agora? (S/N)");
        if (!IsYes(Console.ReadLine())) return false;
        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        Thread.Sleep(1500);
        if (GetRl().Length == 0) return true;
        Console.WriteLine(Ui.C("  Nao consegui fechar. Feche manualmente.", Ui.Red));
        return false;
    }

    private static Process[] GetRl()
    { try { return Process.GetProcessesByName("RocketLeague"); } catch { return Array.Empty<Process>(); } }

    private static bool WriteTest()
    {
        try
        {
            string dir = Path.GetDirectoryName(_cfg!)!;
            string t = Path.Combine(dir, "gutty_wtest.tmp");
            File.WriteAllText(t, "test");
            File.Delete(t);
            return true;
        }
        catch
        {
            Ui.Gap();
            Ui.PanelTop("SEM ACESSO A PASTA");
            Ui.PanelLine(Ui.C("Nao consigo gravar na pasta do jogo.", Ui.Red));
            Ui.PanelLine(Ui.C("Causa provavel: Acesso Controlado a Pastas (Defender)", Ui.Gray));
            Ui.PanelLine(Ui.C("ou antivirus. Veja a secao Antivirus do README.", Ui.Gray));
            Ui.PanelBottom();
            return false;
        }
    }

    private static void Unlock(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        Run("takeown.exe", $"/f \"{path}\"");
        Run("icacls.exe", $"\"{path}\" /reset");
        Run("icacls.exe", $"\"{path}\" /grant \"{Environment.UserName}:(F)\" /c /q");
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
    }

    private static void Run(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            p?.WaitForExit(8000);
        }
        catch { }
    }

    private static void EnsureOriginalBackup()
    {
        if (File.Exists(OrigBackup)) return;
        try
        {
            string text = File.ReadAllText(_cfg!);
            if (text.Contains("GUTTYTECH-RL-OPTIMIZER=") || text.Contains("MaxLODSize=16"))
            { Log("Arquivo atual ja otimizado; original nao capturado - usar stock no REMOVER."); return; }
            File.Copy(_cfg!, OrigBackup, true);
            try { File.SetAttributes(OrigBackup, FileAttributes.Normal); } catch { }
            Log("Backup original pristino criado.");
        }
        catch { }
    }

    private static void Backup()
    {
        try
        {
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(_cfg!, Path.Combine(BackupDir, $"TASystemSettings.{ts}.bak"), true);
        }
        catch { }
    }

    private static Dictionary<string, string> ReadDisplay(string file)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            bool inSs = false;
            foreach (var line in File.ReadAllLines(file))
            {
                if (line.StartsWith('[')) { inSs = line.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase); continue; }
                if (!inSs) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line[..eq];
                foreach (var dk in DisplayKeys)
                    if (key.Equals(dk, StringComparison.OrdinalIgnoreCase) && !d.ContainsKey(dk))
                        d[dk] = line[(eq + 1)..];
            }
        }
        catch { }
        return d;
    }

    private static string ApplyDisplay(string templateText, Dictionary<string, string> disp)
    {
        var sb = new StringBuilder();
        bool inSs = false;
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in templateText.Replace("\r\n", "\n").Split('\n'))
        {
            string outLine = raw;
            if (raw.StartsWith('[')) inSs = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
            else if (inSs)
            {
                int eq = raw.IndexOf('=');
                if (eq > 0)
                {
                    string key = raw[..eq];
                    foreach (var dk in DisplayKeys)
                        if (key.Equals(dk, StringComparison.OrdinalIgnoreCase) && disp.TryGetValue(dk, out var v) && done.Add(dk))
                            outLine = dk + "=" + v;
                }
            }
            sb.Append(outLine).Append("\r\n");
        }
        return sb.ToString();
    }

    private static int FailOrElevate(string mode, bool interactive)
    {
        if (IsAdmin()) { FailPanel(); return 1; }
        Ui.Gap();
        Ui.PanelTop("PRECISA DE ADMINISTRADOR");
        Ui.PanelLine(Ui.C("O arquivo pode estar travado a nivel de SISTEMA", Ui.Amber));
        Ui.PanelLine(Ui.C("por um script antigo que rodou como admin.", Ui.Gray));
        Ui.PanelBottom();
        if (interactive)
        {
            Ui.Prompt("Elevar e tentar como administrador? (S/N)");
            if (!IsYes(Console.ReadLine())) { FailPanel(); return 1; }
        }
        try
        {
            var psi = new ProcessStartInfo(Environment.ProcessPath ?? "")
            { UseShellExecute = true, Verb = "runas", Arguments = mode + " /keepopen" };
            Process.Start(psi);
            Console.WriteLine(Ui.C("  » Abri uma janela de administrador. Pode fechar esta.", Ui.Green));
        }
        catch
        {
            Console.WriteLine(Ui.C("  X Nao consegui elevar. Rode o GuttyRL.exe como administrador.", Ui.Red));
        }
        return 1;
    }

    // -------------------------------------------------------------- Painel de falha
    private static void FailPanel()
    {
        Ui.CompletionMessage(Ui.MRed, "FALHA", new[]
        {
            "Nao consegui aplicar. Seu arquivo NAO foi corrompido.",
            "Backup em: " + FitPath(BackupDir, 45),
            "Cheque antivirus / Acesso Controlado a Pastas."
        });
    }

    private static void Goodbye()
    {
        Ui.Gap();
        Console.WriteLine(Ui.C(new string(' ', Ui.Margin) + "GUTTYTECH - TESSERACT  ", Ui.Red) + Ui.C("// ate a proxima.", Ui.DimC));
        Ui.Gap();
    }

    private static void Log(string msg)
    { try { File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}"); } catch { } }

    private static bool IsYes(string? s) => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
