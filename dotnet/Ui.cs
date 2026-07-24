using System.Text;

namespace GuttyRL;

/// <summary>Camada visual ANSI true-color — rework v22.3.43 (paleta GUTTYTECH).</summary>
internal static partial class Ui
{
    // ---- ANSI base ----
    public const string Reset = "\x1b[0m";
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";
    private const string Hide = "\x1b[?25l";
    private const string ShowCur = "\x1b[?25h";

    private static bool _ansi = true;

    // ---- Paleta (GUTTYTECH) ----
    public static readonly (int r, int g, int b) Red = (229, 10, 10);     // #E50A0A
    public static readonly (int r, int g, int b) RedHi = (255, 78, 78);
    public static readonly (int r, int g, int b) RedLo = (120, 0, 0);
    public static readonly (int r, int g, int b) White = (240, 240, 244);
    public static readonly (int r, int g, int b) Gray = (158, 164, 174);
    public static readonly (int r, int g, int b) DimC = (104, 108, 116);
    public static readonly (int r, int g, int b) Border = (48, 50, 56);
    public static readonly (int r, int g, int b) BorderHi = (72, 74, 82);
    public static readonly (int r, int g, int b) Green = (66, 214, 124);
    public static readonly (int r, int g, int b) Amber = (255, 178, 54);
    public static readonly (int r, int g, int b) Cyan = (60, 206, 224);
    public static readonly (int r, int g, int b) BgDeep = (10, 10, 10);   // #0A0A0A
    public static readonly (int r, int g, int b) BgPanel = (18, 18, 18);  // #121212
    public static readonly (int r, int g, int b) BgRow = (22, 22, 24);

    public static void Init(bool ansiOk)
    {
        _ansi = ansiOk;
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        try
        {
            int maxW = SafeLargestWindowWidth();
            int maxH = SafeLargestWindowHeight();
            int w = Math.Clamp(100, 72, Math.Max(72, maxW));
            int h = Math.Clamp(38, 24, Math.Max(24, maxH));
            if (w <= maxW && h <= maxH)
            {
                try { Console.SetBufferSize(Math.Max(w, Console.BufferWidth), Math.Max(500, Console.BufferHeight)); } catch { }
                try { Console.SetWindowSize(w, h); } catch { }
                try { Console.SetBufferSize(w, Math.Max(500, Console.BufferHeight)); } catch { }
            }
        }
        catch { }
        try { Console.Title = "GUTTYTECH  -  RL INI OPTIMIZER  " + AppMeta.Version; } catch { }
    }

    private static int SafeLargestWindowWidth()
    {
        try { return Math.Max(72, Console.LargestWindowWidth); } catch { return 100; }
    }

    private static int SafeLargestWindowHeight()
    {
        try { return Math.Max(24, Console.LargestWindowHeight); } catch { return 38; }
    }

    public static string Fg((int r, int g, int b) c) => _ansi ? $"\x1b[38;2;{c.r};{c.g};{c.b}m" : "";
    public static string Bg((int r, int g, int b) c) => _ansi ? $"\x1b[48;2;{c.r};{c.g};{c.b}m" : "";
    public static string C(string s, (int r, int g, int b) c) => Fg(c) + s + (_ansi ? Reset : "");
    public static string B(string s, (int r, int g, int b) c) => (_ansi ? Bold : "") + Fg(c) + s + (_ansi ? Reset : "");

    public static void Cls() { try { if (_ansi) Console.Write("\x1b[2J\x1b[3J\x1b[H"); else Console.Clear(); } catch { } }
    public static void HideCursor() { if (_ansi) Console.Write(Hide); }
    public static void ShowCursor() { if (_ansi) Console.Write(ShowCur); }

    private static int WinW { get { try { return Console.WindowWidth; } catch { return 100; } } }
    private static int WinH { get { try { return Console.WindowHeight; } catch { return 40; } } }

    public static string Gradient(string text, (int r, int g, int b) a, (int r, int g, int b) b2)
    {
        if (!_ansi) return text;
        var sb = new StringBuilder();
        int n = Math.Max(1, text.Length - 1);
        for (int i = 0; i < text.Length; i++) sb.Append(Fg(Lerp(a, b2, (double)i / n))).Append(text[i]);
        return sb.Append(Reset).ToString();
    }

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['G'] = new[] { " ██████╗ ", "██╔════╝ ", "██║  ███╗", "██║   ██║", "╚██████╔╝", " ╚═════╝ " },
        ['U'] = new[] { "██╗   ██╗", "██║   ██║", "██║   ██║", "██║   ██║", "╚██████╔╝", " ╚═════╝ " },
        ['T'] = new[] { "████████╗", "╚══██╔══╝", "   ██║   ", "   ██║   ", "   ██║   ", "   ╚═╝   " },
        ['Y'] = new[] { "██╗   ██╗", "╚██╗ ██╔╝", " ╚████╔╝ ", "  ╚██╔╝  ", "   ██║   ", "   ╚═╝   " },
        ['E'] = new[] { "███████╗", "██╔════╝", "█████╗  ", "██╔══╝  ", "███████╗", "╚══════╝" },
        ['C'] = new[] { " ██████╗", "██╔════╝", "██║     ", "██║     ", "╚██████╗", " ╚═════╝" },
        ['H'] = new[] { "██╗  ██╗", "██║  ██║", "███████║", "██╔══██║", "██║  ██║", "╚═╝  ╚═╝" },
    };

    private static string[] BuildWordmark(string word)
    {
        var rows = new string[6];
        for (int i = 0; i < 6; i++)
        {
            var sb = new StringBuilder();
            foreach (char ch in word)
                if (Glyphs.TryGetValue(ch, out var g)) sb.Append(g[i]);
            rows[i] = sb.ToString();
        }
        return rows;
    }

    private static (int r, int g, int b) Lerp((int r, int g, int b) a, (int r, int g, int b) b, double t)
        => ((int)(a.r + (b.r - a.r) * t), (int)(a.g + (b.g - a.g) * t), (int)(a.b + (b.b - a.b) * t));

    public static void Banner(bool animate)
    {
        if (WinH < 30) { BannerCompact(); return; }
        var rows = BuildWordmark("GUTTYTECH");
        int width = rows[0].Length;
        int pad = Math.Max(2, (WinW - width) / 2);
        string margin = new(' ', pad);

        // Faixa superior de marca
        string rule = new string('─', Math.Min(width, WinW - 8));
        int rpad = Math.Max(2, (WinW - rule.Length) / 2);
        Console.WriteLine();
        Console.WriteLine(new string(' ', rpad) + Fg(RedLo) + rule + Reset);
        Console.WriteLine();

        for (int i = 0; i < rows.Length; i++)
        {
            var col = Lerp(RedHi, Red, i / 5.0);
            Console.WriteLine(margin + Fg(col) + rows[i] + (_ansi ? Reset : ""));
            if (animate) Thread.Sleep(38);
        }

        string sub = "ROCKET LEAGUE  -  INI OPTIMIZER";
        string badge = " " + AppMeta.Version + " ";
        string tess = " TESSERACT ";
        int lineW = sub.Length + badge.Length + tess.Length + 6;
        int spad = Math.Max(2, (WinW - lineW) / 2);
        Console.WriteLine();
        Console.WriteLine(
            new string(' ', spad)
            + C(sub, Gray)
            + "  "
            + Bg(Red) + Bold + Fg(White) + badge + Reset
            + " "
            + Fg(RedHi) + tess + Reset);
        Console.WriteLine(new string(' ', rpad) + Fg(RedLo) + rule + Reset);
        Console.WriteLine();
    }

    private static void BannerCompact()
    {
        string word = "G U T T Y T E C H";
        int pad = Math.Max(2, (WinW - word.Length) / 2);
        string m = new(' ', pad);
        Console.WriteLine();
        Console.WriteLine(m + Bold + Gradient(word, RedHi, Red) + Reset);
        Console.WriteLine(
            m + C("RL INI OPTIMIZER", Gray)
            + "  " + Bg(Red) + Fg(White) + Bold + " " + AppMeta.Version + " " + Reset
            + " " + C("TESSERACT", RedHi));
        Console.WriteLine();
    }

    public static void Intro()
    {
        if (!_ansi) return;
        HideCursor();
        Cls();
        Banner(animate: true);

        int total = 28;
        string label = "BOOT SEQUENCE";
        int pad = Math.Max(2, (WinW - total - label.Length - 12) / 2);
        string m = new(' ', pad);
        string[] ticks = { "#", "-" };

        for (int i = 0; i <= total; i++)
        {
            int p = i * 100 / total;
            var fillCol = Lerp(RedLo, RedHi, (double)i / total);
            var sb = new StringBuilder();
            for (int k = 0; k < total; k++)
            {
                if (k < i) sb.Append(Fg(fillCol)).Append(ticks[0]);
                else sb.Append(Fg(Border)).Append(ticks[1]);
            }
            Console.Write("\r" + m + C(label + "  ", DimC) + sb + Reset + C($"  {p,3}%", Gray));
            Thread.Sleep(12);
        }
        Console.WriteLine();
        Thread.Sleep(90);
        ShowCursor();
    }

    public static int VisLen(string s)
    {
        int n = 0; bool esc = false;
        foreach (char ch in s)
        {
            if (esc) { if (ch == 'm') esc = false; continue; }
            if (ch == '\x1b') { esc = true; continue; }
            n++;
        }
        return n;
    }

    public static int Margin => Math.Max(2, (WinW - 78) / 2);
    private const int Inner = 74;

    public static void PanelTop(string title)
    {
        string m = new(' ', Margin);
        string t = " " + title + " ";
        string left = "╭─";
        int rest = Inner - 1 - t.Length;
        if (rest < 0) rest = 0;
        Console.WriteLine(m + Fg(BorderHi) + left + Reset + B(t, Red) + Fg(BorderHi) + new string('─', rest) + "╮" + Reset);
    }

    public static void PanelLine(string content)
    {
        string m = new(' ', Margin);
        int padLen = Inner - VisLen(content);
        if (padLen < 0) padLen = 0;
        Console.WriteLine(
            m + Fg(BorderHi) + "│" + Reset
            + Bg(BgPanel) + " " + content + new string(' ', padLen) + Reset
            + Fg(BorderHi) + "│" + Reset);
    }

    public static void PanelBlank() => PanelLine("");

    public static void PanelBottom()
    {
        string m = new(' ', Margin);
        Console.WriteLine(m + Fg(BorderHi) + "╰" + new string('─', Inner + 1) + "╯" + Reset);
    }

    public static void Gap() => Console.WriteLine();

    /// <summary>Chip/badge com fundo colorido.</summary>
    public static string Chip(string text, (int r, int g, int b) bg, (int r, int g, int b)? fg = null)
    {
        var f = fg ?? White;
        return Bg(bg) + Bold + Fg(f) + " " + text + " " + Reset;
    }

    public static string Dot((int r, int g, int b) c, string text) => C("*", c) + " " + C(text, White);

    public static string Field(string label, string valueColored)
        => C(label.PadRight(10), DimC) + valueColored;

    /// <summary>Card de opcao do menu: badge numerado + titulo + descricao.</summary>
    public static void MenuCard(string n, string title, string desc, (int r, int g, int b) accent, string? tag = null)
    {
        string badge = Bg(accent) + Bold + Fg(White) + " " + n + " " + Reset;
        string tagPart = string.IsNullOrEmpty(tag)
            ? ""
            : "  " + Fg(accent) + "-" + Reset + " " + C(tag, accent);
        PanelLine(badge + "  " + B(title, White) + tagPart);
        PanelLine("     " + C(desc, DimC));
    }

    public static bool Step(string label, Func<bool> work)
    {
        string m = new(' ', Margin);
        Console.Write(m + "  " + C("o", DimC) + " " + C(label + "...", Gray));
        bool ok; try { ok = work(); } catch { ok = false; }
        Thread.Sleep(70);
        string mark = ok ? C("+", Green) : C("x", Red);
        Console.Write("\r" + m + "  " + mark + " " + C(label, ok ? White : Red) + new string(' ', 16) + "\n");
        return ok;
    }

    public static void Prompt(string text)
    {
        ShowCursor();
        string m = new(' ', Margin);
        Console.Write("\n" + m + Bg(Red) + Fg(White) + Bold + " > " + Reset + " " + B(text, White) + " ");
    }

    public static void PressEnter()
    {
        ShowCursor();
        string m = new(' ', Margin);
        Console.Write("\n" + m + C("  pressione ", DimC) + B("ENTER", White) + C(" para continuar", DimC) + "  ");
        WaitForEnter();
    }

    public static void SectionTitle(string text, (int r, int g, int b) accent)
    {
        Cls();
        Gap();
        string m = new(' ', Margin);
        Console.WriteLine(m + Bg(accent) + Fg(White) + Bold + "  " + text + "  " + Reset);
        Gap();
    }

    public static void FooterHint(string text)
    {
        string m = new(' ', Margin);
        Console.WriteLine();
        Console.WriteLine(m + Fg(DimC) + "  " + text + Reset);
    }
}
