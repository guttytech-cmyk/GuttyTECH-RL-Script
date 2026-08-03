namespace GuttyRL;

/// <summary>Componentes de fluxo, progresso e conclusão do design system.</summary>
internal static partial class Ui
{
    public static readonly (int r, int g, int b) MRed = Red;
    public static readonly (int r, int g, int b) MCyan = Cyan;
    public static readonly (int r, int g, int b) MAmber = Amber;
    public static readonly (int r, int g, int b) OkGreen = Green;
    public static readonly (int r, int g, int b) WarnYel = Amber;
    public static readonly (int r, int g, int b) LightGray = Gray;
    public static readonly (int r, int g, int b) DarkGray = DimC;
    private static readonly (int r, int g, int b) BtnBg = BgRow;
    private static readonly (int r, int g, int b) BtnBorder = BorderHi;
    private static readonly (int r, int g, int b) SpinGray = DimC;

    private static readonly string[] Spin = { "◐", "◓", "◑", "◒" };

    private static int CW => Inner;
    private static string CMar => new(' ', Margin);

    public static (int r, int g, int b) ModeColor(string m) => m == "COMPLETO" ? MRed : m == "CRIADOR" ? MCyan : MAmber;

    public static void FlushInput() { try { while (Console.KeyAvailable) Console.ReadKey(true); } catch { } }

    private static string Raw(string t, (int r, int g, int b) c) => Fg(c) + t;
    private static string RawB(string t, (int r, int g, int b) c) =>
        (_ansi ? Bold : "") + Fg(c) + t + (_ansi ? "\x1b[22m" : "");
    private static string CenterRaw(string plain, (int r, int g, int b) c, bool bold = false)
    {
        int left = Math.Max(0, (CW - plain.Length) / 2);
        string s = new string(' ', left) + plain;
        return bold ? RawB(s, c) : Raw(s, c);
    }
    private static string Trunc(string s, int max) => s.Length <= max ? s : "..." + s[^(max - 3)..];

    public static void MiniBannerIfTall((int r, int g, int b) acc)
    {
        if (WinH < 48) return;
        string brand = "GUTTYTECH";
        string sub = CW >= 30 ? "RL OPTIMIZER  /  " + AppMeta.Version : "RL  /  " + AppMeta.Version;
        int w = Math.Min(CW, 44);
        string m = new(' ', Math.Max(2, (WinW - w - 2) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(BorderHi) + "╭" + new string('─', w) + "╮" + Reset);
        int bp = Math.Max(0, (w - brand.Length) / 2);
        Console.WriteLine(m + Fg(BorderHi) + "│" + Reset + Bg(BgPanel)
            + new string(' ', bp) + RawB(brand, acc) + new string(' ', w - bp - brand.Length)
            + Reset + Fg(acc) + "│" + Reset);
        int sp = Math.Max(0, (w - sub.Length) / 2);
        Console.WriteLine(m + Fg(BorderHi) + "│" + Reset + Bg(BgPanel) + new string(' ', sp) + C(sub, Gray)
            + Bg(BgPanel) + new string(' ', w - sp - sub.Length) + Reset + Fg(BorderHi) + "│" + Reset);
        Console.WriteLine(m + Fg(BorderHi) + "╰" + new string('─', w) + "╯" + Reset);
    }

    public static void TitleBar(string text, (int r, int g, int b) acc)
    {
        string m = CMar;
        string title = text.ToUpperInvariant();
        Console.WriteLine();
        Console.WriteLine(m + Fg(acc) + "╭━" + Reset + " " + Chip("GUTTYTECH", acc)
            + " " + Fg(BorderHi) + new string('━', Math.Max(0, CW - 14)) + "╮" + Reset);
        string body = "  " + title;
        string fitted = FitAnsi(body, CW - 2);
        int pad = Math.Max(0, CW - VisLen(fitted));
        Console.WriteLine(m + Fg(BorderHi) + "│" + Bg(BgPanel) + Bold + Fg(White) + fitted
            + new string(' ', pad) + Reset + Fg(BorderHi) + "│" + Reset);
        Console.WriteLine(m + Fg(BorderHi) + "╰" + new string('─', CW) + "╯" + Reset);
        Console.WriteLine();
    }

    public static bool StepAnimated(string label, Func<bool> work)
    {
        string m = CMar;
        HideCursor();
        using var completed = new ManualResetEventSlim(false);
        bool ok = false;
        var worker = new Thread(() =>
        {
            try { ok = work(); }
            catch { ok = false; }
            finally { completed.Set(); }
        })
        { IsBackground = true };
        worker.Start();

        int i = 0;
        while (!completed.IsSet)
        {
            Console.Write("\r" + m + "  " + Fg(SpinGray) + Spin[i % Spin.Length] + Reset
                + " " + Fg(LightGray) + FitAnsi(label + "...", Math.Max(10, CW - 8)) + Reset + "   ");
            Thread.Sleep(55);
            i++;
        }
        worker.Join();

        for (int j = 0; j < 3; j++)
        {
            var sc = Lerp(SpinGray, ok ? OkGreen : MRed, (j + 1) / 3.0);
            Console.Write("\r" + m + "  " + Fg(sc) + Spin[(i + j) % Spin.Length] + Reset
                + " " + Fg(LightGray) + FitAnsi(label + "...", Math.Max(10, CW - 8)) + Reset + "   ");
            Thread.Sleep(18);
        }
        string mark = ok ? Fg(OkGreen) + "◆" : Fg(MRed) + "×";
        Console.WriteLine("\r" + m + "  " + mark + Reset + " " + Fg(White)
            + FitAnsi(label, Math.Max(10, CW - 8)) + Reset + new string(' ', 24));
        Thread.Sleep(30);
        return ok;
    }

    /// <summary>Step com barra de progresso ao vivo (callback: texto curto / BAR).</summary>
    public static bool StepWithBar(string label, Func<Action<int, int, string>, bool> work)
    {
        string m = CMar;
        HideCursor();
        FlushInput();
        int cur = 0, total = 1;
        string detail = "";
        object gate = new();
        using var completed = new ManualResetEventSlim(false);
        bool ok = false;

        void SetBar(int c, int t, string d)
        {
            lock (gate)
            {
                cur = Math.Max(0, c);
                total = Math.Max(1, t);
                detail = d ?? "";
            }
        }

        var worker = new Thread(() =>
        {
            try { ok = work(SetBar); }
            catch { ok = false; }
            finally { completed.Set(); }
        })
        { IsBackground = true };
        worker.Start();

        int i = 0;
        int barW = Math.Clamp(CW - 42, 12, 28);
        while (!completed.IsSet)
        {
            int c, t; string d;
            lock (gate) { c = cur; t = total; d = detail; }
            double pct = Math.Clamp(100.0 * c / t, 0, 100);
            int filled = (int)Math.Round(barW * pct / 100.0);
            if (filled > barW) filled = barW;
            string bar = new string('━', filled) + new string('─', barW - filled);
            int detailWidth = Math.Max(6, Math.Min(18, CW / 4));
            string line = $"{label}  [{bar}]  {(int)pct,3}%  {c}/{t}  {Trunc(d, detailWidth)}";
            Console.Write("\r" + m + "  " + Fg(SpinGray) + Spin[i % Spin.Length] + Reset
                + " " + Fg(LightGray) + FitAnsi(line, Math.Max(10, CW - 4)) + Reset + "    ");
            Thread.Sleep(50);
            i++;
        }
        worker.Join();

        string mark = ok ? Fg(OkGreen) + "◆" : Fg(MRed) + "×";
        string finalBar = ok ? new string('━', barW) : new string('─', barW);
        string final = Fg(White) + label + Reset
            + "  " + Fg(ok ? OkGreen : MRed) + "[" + finalBar + "]" + Reset
            + Fg(LightGray) + (ok ? "  100%" : "  falhou") + Reset;
        Console.WriteLine("\r" + m + "  " + mark + Reset + " "
            + FitAnsi(final, Math.Max(10, CW - 4)) + new string(' ', 20));
        FlushInput();
        Thread.Sleep(30);
        return ok;
    }

    private static void PanelLineBg((int r, int g, int b) acc, (int r, int g, int b) bg, string content)
    {
        string fitted = FitAnsi(content, CW);
        int pad = Math.Max(0, CW - VisLen(fitted));
        Console.WriteLine(CMar + Fg(BorderHi) + "│" + Bg(bg) + fitted + new string(' ', pad)
            + Reset + Fg(BorderHi) + "│" + Reset);
    }

    private static void DrawPanel((int r, int g, int b) acc, List<string> content, bool reveal)
    {
        string m = CMar;
        Console.WriteLine(m + Fg(acc) + "╭" + new string('━', CW) + "╮" + Reset);
        int n = content.Count;
        for (int i = 0; i < n; i++)
        {
            PanelLineBg(acc, BgPanel, content[i]);
            if (reveal) Thread.Sleep(8);
        }
        Console.WriteLine(m + Fg(BorderHi) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    private static void DrawPanelTitled((int r, int g, int b) acc, string title, List<string> content, bool reveal)
    {
        string m = CMar;
        string titleFit = title.ToUpperInvariant();
        int maxTitle = Math.Max(4, CW - 5);
        if (titleFit.Length > maxTitle) titleFit = titleFit[..Math.Max(1, maxTitle - 1)] + "…";
        string t = " " + titleFit + " ";
        int rest = CW - 1 - t.Length; if (rest < 0) rest = 0;
        Console.WriteLine(m + Fg(BorderHi) + "╭─" + Reset + RawB(t, acc) + Fg(BorderHi) + new string('─', rest) + "╮" + Reset);
        int n = content.Count;
        for (int i = 0; i < n; i++)
        {
            PanelLineBg(acc, BgPanel, content[i]);
            if (reveal) Thread.Sleep(8);
        }
        Console.WriteLine(m + Fg(BorderHi) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    public static void CompletionSuccess(string mode, (int r, int g, int b) acc, string backupPath)
    {
        string[] tips = mode == "CRIADOR"
            ? new[]
            {
                "Limpa + aplica + sync dos perfis recentes",
                "Nao cliques APLICAR em resolucao/modo — o RL reseta o menu",
            }
            : new[]
            {
                "Menu: Desempenho / Alto desempenho / FPS Unlimited",
                "Nao cliques APLICAR em resolucao/modo — o RL reseta o menu",
            };

        var c = new List<string>
        {
            "",
            CenterRaw("◆  OPERAÇÃO CONCLUÍDA  ◆", acc, true),
            CenterRaw(new string('─', 16), acc),
            "",
            "  " + Raw("MODO ", LightGray) + RawB(mode, acc) + Raw("  -  ", DarkGray) + RawB("OK", OkGreen),
            "",
        };
        foreach (var tip in tips)
            foreach (string line in WrapPlain(tip, CW - 7))
                c.Add("  " + Raw("› ", acc) + Raw(line, LightGray));
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
            CenterRaw("◆  " + title + "  ◆", acc, true),
            CenterRaw(new string('─', Math.Min(CW - 8, title.Length + 8)), acc),
            ""
        };
        foreach (var ln in lines)
            foreach (string line in WrapPlain(ln, CW - 4))
                c.Add("  " + Raw(line, LightGray));
        c.Add("");
        DrawPanel(acc, c, reveal: true);
    }

    public static void EnterButton()
    {
        FlushInput();
        int bw = Math.Min(40, Math.Max(20, CW - 4));
        string m = new(' ', Math.Max(2, (WinW - bw - 2) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(BtnBorder) + "╭" + new string('─', bw) + "╮" + Reset);
        string left = bw >= 34 ? "  PRESSIONE " : "  ";
        string key = "ENTER";
        string right = bw >= 34 ? " PARA CONTINUAR  " : "  /  CONTINUAR  ";
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
        string fitted = FitAnsi(text, CW - 2);
        int pad = Math.Max(0, CW - VisLen(fitted) - 2);
        Console.WriteLine(m + Fg(DarkGray) + "│" + Bg(BgPanel) + " " + Bold + Fg(OkGreen) + fitted
            + (_ansi ? "\x1b[22m" : "") + new string(' ', pad) + " " + Reset + Fg(DarkGray) + "│" + Reset);
        Console.WriteLine(m + Fg(DarkGray) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    public static void CopyStatus(bool ok)
    {
        string m = CMar;
        if (ok)
            Console.WriteLine(m + "  " + Fg(OkGreen) + Bold + "◆ COPIADO" + (_ansi ? "\x1b[22m" : "") + Reset
                + Fg(LightGray) + "  Cole com " + Reset + Fg(White) + "Ctrl+V" + Reset
                + Fg(LightGray) + " no launcher." + Reset);
        else
            Console.WriteLine(m + "  " + Fg(MAmber) + Bold + "▲ CÓPIA MANUAL" + Reset
                + Fg(LightGray) + " — selecione o comando e Ctrl+C." + Reset);
    }

    public static void LaunchHeading(string text)
    {
        Console.WriteLine();
        Console.WriteLine(CMar + Bold + Fg(White) + "  " + text.ToUpperInvariant()
            + (_ansi ? "\x1b[22m" : "") + Reset);
    }

    public static void LaunchParam(string sym, (int r, int g, int b) color, string label, string desc)
    {
        int labelWidth = Math.Min(22, Math.Max(14, CW / 3));
        string line = "  " + Fg(color) + sym + Reset + " " + Fg(White) + label.PadRight(labelWidth) + Reset
            + Fg(DarkGray) + desc + Reset;
        Console.WriteLine(CMar + FitAnsi(line, CW));
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
            string prefix = i.ToString("00") + "  ";
            bool first = true;
            foreach (string line in WrapPlain(s, CW - 8))
            {
                c.Add("  " + Raw(first ? prefix : "    ", first ? acc : DarkGray) + Raw(line, LightGray));
                first = false;
            }
            i++;
        }
        c.Add("");
        DrawPanelTitled(acc, title, c, reveal: false);
    }
}
