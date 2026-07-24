namespace GuttyRL;

/// <summary>Telas de fluxo: titulo, steps, conclusao, launch options — rework v22.3.43.</summary>
internal static partial class Ui
{
    public static readonly (int r, int g, int b) MRed = (255, 42, 42);
    public static readonly (int r, int g, int b) MCyan = (0, 229, 255);
    public static readonly (int r, int g, int b) MAmber = (255, 179, 0);
    public static readonly (int r, int g, int b) OkGreen = (0, 255, 65);
    public static readonly (int r, int g, int b) WarnYel = (255, 215, 0);
    public static readonly (int r, int g, int b) LightGray = (192, 192, 192);
    public static readonly (int r, int g, int b) DarkGray = (102, 102, 102);
    private static readonly (int r, int g, int b) PanelTopBg = (28, 28, 30);
    private static readonly (int r, int g, int b) PanelBotBg = (14, 14, 16);
    private static readonly (int r, int g, int b) BtnBg = (36, 36, 40);
    private static readonly (int r, int g, int b) BtnBorder = (90, 90, 98);
    private static readonly (int r, int g, int b) SpinGray = (140, 140, 148);

    private static readonly string[] Spin = { "|", "/", "-", "\\", "|", "/", "-", "\\", "|", "/" };

    private const int CW = 62;
    private static string CMar => new(' ', Math.Max(2, (WinW - CW - 2) / 2));

    public static (int r, int g, int b) ModeColor(string m) => m == "COMPLETO" ? MRed : m == "CRIADOR" ? MCyan : MAmber;

    public static void FlushInput() { try { while (Console.KeyAvailable) Console.ReadKey(true); } catch { } }

    private static string Raw(string t, (int r, int g, int b) c) => Fg(c) + t;
    private static string RawB(string t, (int r, int g, int b) c) => "\x1b[1m" + Fg(c) + t + "\x1b[22m";
    private static string CenterRaw(string plain, (int r, int g, int b) c, bool bold = false)
    {
        int left = Math.Max(0, (CW - plain.Length) / 2);
        string s = new string(' ', left) + plain;
        return bold ? RawB(s, c) : Raw(s, c);
    }
    private static string Trunc(string s, int max) => s.Length <= max ? s : "..." + s[^(max - 3)..];

    public static void MiniBannerIfTall((int r, int g, int b) acc)
    {
        if (WinH < 34) return;
        string brand = "GUTTYTECH";
        string sub = "ROCKET LEAGUE  -  " + AppMeta.Version;
        int w = Math.Max(brand.Length, sub.Length) + 8;
        string m = new(' ', Math.Max(2, (WinW - w) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(acc) + "╭" + new string('─', w - 2) + "╮" + Reset);
        int bp = Math.Max(0, (w - 2 - brand.Length) / 2);
        Console.WriteLine(m + Fg(acc) + "│" + Reset + Bg(Lerp(acc, BgDeep, 0.85))
            + new string(' ', bp) + RawB(brand, White) + new string(' ', w - 2 - bp - brand.Length)
            + Reset + Fg(acc) + "│" + Reset);
        int sp = Math.Max(0, (w - 2 - sub.Length) / 2);
        Console.WriteLine(m + Fg(acc) + "│" + Reset + new string(' ', sp) + C(sub, Gray)
            + new string(' ', w - 2 - sp - sub.Length) + Fg(acc) + "│" + Reset);
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', w - 2) + "╯" + Reset);
    }

    public static void TitleBar(string text, (int r, int g, int b) acc)
    {
        string m = CMar;
        var glow = Lerp(acc, BgDeep, 0.82);
        Console.WriteLine();
        Console.WriteLine(m + Fg(acc) + "╭" + new string('─', CW) + "╮" + Reset);
        string body = "  " + text;
        int pad = CW - body.Length; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(acc) + "│" + Bg(glow) + "\x1b[1m" + Fg(White) + body
            + new string(' ', pad) + Reset + Fg(acc) + "│" + Reset);
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', CW) + "╯" + Reset);
        Console.WriteLine();
    }

    public static bool StepAnimated(string label, Func<bool> work)
    {
        string m = CMar;
        HideCursor();
        bool done = false;
        bool ok = false;
        var worker = new Thread(() =>
        {
            try { ok = work(); }
            catch { ok = false; }
            finally { done = true; }
        })
        { IsBackground = true };
        worker.Start();

        int i = 0;
        while (!done)
        {
            Console.Write("\r" + m + "  " + Fg(SpinGray) + Spin[i % Spin.Length] + Reset
                + " " + Fg(LightGray) + label + "..." + Reset + "   ");
            Thread.Sleep(70);
            i++;
        }
        worker.Join();

        for (int j = 0; j < 6; j++)
        {
            var sc = Lerp(SpinGray, ok ? OkGreen : MRed, (j + 1) / 6.0);
            Console.Write("\r" + m + "  " + Fg(sc) + Spin[(i + j) % Spin.Length] + Reset
                + " " + Fg(LightGray) + label + "..." + Reset + "   ");
            Thread.Sleep(28);
        }
        string mark = ok ? Fg(OkGreen) + "+" : Fg(MRed) + "x";
        Console.WriteLine("\r" + m + "  " + mark + Reset + " " + Fg(White) + label + Reset + new string(' ', 24));
        Thread.Sleep(80);
        return ok;
    }

    /// <summary>Step com progresso ao vivo (callback atualiza o texto a direita).</summary>
    public static bool StepAnimatedProgress(string label, Func<Action<string>, bool> work)
    {
        string m = CMar;
        HideCursor();
        string status = "";
        object gate = new();
        bool done = false;
        bool ok = false;

        void SetStatus(string s)
        {
            lock (gate) status = s;
        }

        var worker = new Thread(() =>
        {
            try { ok = work(SetStatus); }
            catch { ok = false; }
            finally { done = true; }
        })
        { IsBackground = true };
        worker.Start();

        int i = 0;
        while (!done)
        {
            string st;
            lock (gate) st = status;
            string line = label + (string.IsNullOrEmpty(st) ? "..." : "  " + st);
            Console.Write("\r" + m + "  " + Fg(SpinGray) + Spin[i % Spin.Length] + Reset
                + " " + Fg(LightGray) + line + Reset + "          ");
            Thread.Sleep(70);
            i++;
        }
        worker.Join();

        string mark = ok ? Fg(OkGreen) + "+" : Fg(MRed) + "x";
        Console.WriteLine("\r" + m + "  " + mark + Reset + " " + Fg(White) + label + Reset + new string(' ', 36));
        Thread.Sleep(80);
        return ok;
    }

    private static void PanelLineBg((int r, int g, int b) acc, (int r, int g, int b) bg, string content)
    {
        int pad = CW - VisLen(content); if (pad < 0) pad = 0;
        Console.WriteLine(CMar + Fg(acc) + "│" + Bg(bg) + content + new string(' ', pad) + Reset + Fg(acc) + "│" + Reset);
    }

    private static void DrawPanel((int r, int g, int b) acc, List<string> content, bool reveal)
    {
        string m = CMar;
        Console.WriteLine(m + Fg(acc) + "╭" + new string('─', CW) + "╮" + Reset);
        int n = content.Count;
        for (int i = 0; i < n; i++)
        {
            var bg = Lerp(PanelTopBg, PanelBotBg, n <= 1 ? 0.0 : (double)i / (n - 1));
            PanelLineBg(acc, bg, content[i]);
            if (reveal) Thread.Sleep(28);
        }
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    private static void DrawPanelTitled((int r, int g, int b) acc, string title, List<string> content, bool reveal)
    {
        string m = CMar;
        string t = " " + title + " ";
        int rest = CW - 1 - t.Length; if (rest < 0) rest = 0;
        Console.WriteLine(m + Fg(acc) + "╭─" + Reset + RawB(t, acc) + Fg(acc) + new string('─', rest) + "╮" + Reset);
        int n = content.Count;
        for (int i = 0; i < n; i++)
        {
            var bg = Lerp(PanelTopBg, PanelBotBg, n <= 1 ? 0.0 : (double)i / (n - 1));
            PanelLineBg(acc, bg, content[i]);
            if (reveal) Thread.Sleep(28);
        }
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    public static void CompletionSuccess(string mode, (int r, int g, int b) acc, string backupPath)
    {
        string[] tips = mode == "CRIADOR"
            ? new[]
            {
                "Sempre limpa (REMOVER) + aplica + sync de todas as contas",
                "Trocou de conta? Feche o RL e reabra o GuttyTECH (auto-heal)",
            }
            : new[]
            {
                "Menu: Desempenho / Alto desempenho / FPS Unlimited",
                "Trocou de conta? Feche o RL e reabra o GuttyTECH (auto-heal)",
            };

        var c = new List<string>
        {
            "",
            CenterRaw("*  CONCLUIDO  *", acc, true),
            CenterRaw(new string('─', 16), acc),
            "",
            "  " + Raw("MODO ", LightGray) + RawB(mode, acc) + Raw("  -  ", DarkGray) + RawB("OK", OkGreen),
            "",
        };
        foreach (var tip in tips)
            c.Add("  " + Raw("> ", acc) + Raw(tip, LightGray));
        c.Add("");
        c.Add("  " + Raw("Backups  ", DimC) + Raw(Trunc(backupPath, CW - 14), DarkGray));
        c.Add("");
        DrawPanel(acc, c, reveal: true);
    }

    public static void CompletionMessage((int r, int g, int b) acc, string title, string[] lines)
    {
        var c = new List<string>
        {
            "",
            CenterRaw("*  " + title + "  *", acc, true),
            CenterRaw(new string('─', Math.Min(CW - 8, title.Length + 8)), acc),
            ""
        };
        foreach (var ln in lines) c.Add("  " + Raw(ln, LightGray));
        c.Add("");
        DrawPanel(acc, c, reveal: true);
    }

    public static void EnterButton()
    {
        FlushInput();
        const int bw = 38;
        string m = new(' ', Math.Max(2, (WinW - bw - 2) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(BtnBorder) + "╭" + new string('─', bw) + "╮" + Reset);
        string left = "  pressione ";
        string key = "ENTER";
        string right = " para continuar  ";
        int inner = left.Length + key.Length + right.Length;
        int pad = bw - inner; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(BtnBorder) + "│" + Bg(BtnBg)
            + Fg(LightGray) + left
            + RawB(key, WarnYel)
            + Fg(LightGray) + right
            + new string(' ', pad)
            + Reset + Fg(BtnBorder) + "│" + Reset);
        Console.WriteLine(m + Fg(BtnBorder) + "╰" + new string('─', bw) + "╯" + Reset);
        WaitForEnter();
    }

    private static void WaitForEnter()
    {
        try
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    return;
            }
        }
        catch
        {
            StartupGuard.WaitForUser();
        }
    }

    public static void CodeBox(string text)
    {
        string m = CMar;
        Console.WriteLine(m + Fg(DarkGray) + "╭" + new string('─', CW) + "╮" + Reset);
        int pad = CW - text.Length - 2; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(DarkGray) + "│" + Bg((20, 22, 20)) + " \x1b[1m" + Fg(OkGreen) + text
            + "\x1b[22m" + new string(' ', pad) + " " + Reset + Fg(DarkGray) + "│" + Reset);
        Console.WriteLine(m + Fg(DarkGray) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    public static void CopyStatus(bool ok)
    {
        string m = CMar;
        if (ok)
            Console.WriteLine(m + "  " + Fg(OkGreen) + "\x1b[1m+ Copiado!\x1b[22m" + Reset
                + Fg(LightGray) + "  Cole com " + Reset + Fg(White) + "Ctrl+V" + Reset
                + Fg(LightGray) + " no launcher." + Reset);
        else
            Console.WriteLine(m + "  " + Fg(MAmber) + "! Nao copiou sozinho" + Reset
                + Fg(LightGray) + " — selecione o comando e Ctrl+C." + Reset);
    }

    public static void LaunchHeading(string text)
    {
        Console.WriteLine();
        Console.WriteLine(CMar + "\x1b[1m" + Fg(LightGray) + text + "\x1b[22m" + Reset);
    }

    public static void LaunchParam(string sym, (int r, int g, int b) color, string label, string desc)
    {
        Console.WriteLine(CMar + "  " + Fg(color) + sym + Reset + " " + Fg(White) + label.PadRight(22) + Reset
            + Fg(DarkGray) + desc + Reset);
    }

    public static void LaunchNote(string text)
    {
        Console.WriteLine();
        Console.WriteLine(CMar + Fg(DarkGray) + text + Reset);
    }

    public static void StepsPanel(string title, string[] steps, (int r, int g, int b) acc)
    {
        var c = new List<string> { "" };
        int i = 1;
        foreach (var s in steps)
        {
            c.Add("  " + Raw(i.ToString().PadLeft(2) + ". ", acc) + Raw(s, LightGray));
            i++;
        }
        c.Add("");
        DrawPanelTitled(acc, title, c, reveal: false);
    }
}
