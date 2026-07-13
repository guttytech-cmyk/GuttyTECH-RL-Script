using System.Text;

namespace GuttyRL;

/// <summary>Camada visual ANSI true-color (paleta GUTTYTECH). Banner, paineis, badges, steps.</summary>
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
    public static readonly (int r, int g, int b) Border = (58, 60, 66);
    public static readonly (int r, int g, int b) Green = (66, 214, 124);
    public static readonly (int r, int g, int b) Amber = (255, 178, 54);
    public static readonly (int r, int g, int b) Cyan = (60, 206, 224);

    public static void Init(bool ansiOk)
    {
        _ansi = ansiOk;
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        try
        {
            int maxW = SafeLargestWindowWidth();
            int maxH = SafeLargestWindowHeight();
            int w = Math.Clamp(98, 72, Math.Max(72, maxW));
            int h = Math.Clamp(36, 24, Math.Max(24, maxH));
            if (w <= maxW && h <= maxH)
            {
                try { Console.SetBufferSize(Math.Max(w, Console.BufferWidth), Math.Max(500, Console.BufferHeight)); } catch { }
                try { Console.SetWindowSize(w, h); } catch { }
                try { Console.SetBufferSize(w, Math.Max(500, Console.BufferHeight)); } catch { }
            }
        }
        catch { }
    }

    private static int SafeLargestWindowWidth()
    {
        try { return Math.Max(72, Console.LargestWindowWidth); } catch { return 98; }
    }

    private static int SafeLargestWindowHeight()
    {
        try { return Math.Max(24, Console.LargestWindowHeight); } catch { return 36; }
    }

    public static string Fg((int r, int g, int b) c) => _ansi ? $"\x1b[38;2;{c.r};{c.g};{c.b}m" : "";
    public static string Bg((int r, int g, int b) c) => _ansi ? $"\x1b[48;2;{c.r};{c.g};{c.b}m" : "";
    public static string C(string s, (int r, int g, int b) c) => Fg(c) + s + (_ansi ? Reset : "");
    public static string B(string s, (int r, int g, int b) c) => (_ansi ? Bold : "") + Fg(c) + s + (_ansi ? Reset : "");

    public static void Cls() { try { if (_ansi) Console.Write("\x1b[2J\x1b[3J\x1b[H"); else Console.Clear(); } catch { } }
    public static void HideCursor() { if (_ansi) Console.Write(Hide); }
    public static void ShowCursor() { if (_ansi) Console.Write(ShowCur); }

    private static int WinW { get { try { return Console.WindowWidth; } catch { return 98; } } }
    private static int WinH { get { try { return Console.WindowHeight; } catch { return 40; } } }

    /// <summary>Gradiente horizontal: colore cada caractere de 'a' ate 'b'.</summary>
    public static string Gradient(string text, (int r, int g, int b) a, (int r, int g, int b) b2)
    {
        if (!_ansi) return text;
        var sb = new StringBuilder();
        int n = Math.Max(1, text.Length - 1);
        for (int i = 0; i < text.Length; i++) sb.Append(Fg(Lerp(a, b2, (double)i / n))).Append(text[i]);
        return sb.Append(Reset).ToString();
    }

    // ---- Banner (wordmark construido a partir de glifos por letra) ----
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
        if (WinH < 30) { BannerCompact(); return; }   // notebooks baixos: nao empurra o menu
        var rows = BuildWordmark("GUTTYTECH");
        int width = rows[0].Length;
        int pad = Math.Max(2, (WinW - width) / 2);
        string margin = new(' ', pad);
        Console.WriteLine();
        for (int i = 0; i < rows.Length; i++)
        {
            var col = Lerp(RedHi, RedLo, i / 5.0);
            Console.WriteLine(margin + Fg(col) + rows[i] + (_ansi ? Reset : ""));
            if (animate) Thread.Sleep(45);
        }
        // subtitulo letter-spaced + tag
        string sub = "R O C K E T   L E A G U E   ·   I N I   O P T I M I Z E R";
        int spad = Math.Max(2, (WinW - sub.Length - 18) / 2);
        Console.WriteLine();
        Console.WriteLine(new string(' ', spad) + C(sub, Gray) + "   " + Bg(Red) + Fg(White) + " v22.3.3 " + Reset + " " + C("TESSERACT", RedHi));
        Console.WriteLine();
    }

    private static void BannerCompact()
    {
        string word = "G U T T Y T E C H";
        int pad = Math.Max(2, (WinW - word.Length) / 2);
        string m = new(' ', pad);
        Console.WriteLine();
        Console.WriteLine(m + Bold + Gradient(word, RedHi, RedLo) + Reset);
        Console.WriteLine(m + C("RL INI OPTIMIZER", Gray) + "  " + Bg(Red) + Fg(White) + " v22.3.3 " + Reset + " " + C("TESSERACT", RedHi));
        Console.WriteLine();
    }

    public static void Intro()
    {
        if (!_ansi) return;
        HideCursor();
        Cls();
        Banner(animate: true);
        // barra de carregamento
        int total = 34;
        int pad = Math.Max(2, (WinW - total - 24) / 2);
        string m = new(' ', pad);
        for (int i = 0; i <= total; i++)
        {
            int p = i * 100 / total;
            string filled = new('▰', i);
            string empty = new('▱', total - i);
            Console.Write("\r" + m + C("INICIALIZANDO  ", DimC) + Fg(Red) + filled + Fg(Border) + empty + Reset + C($"  {p,3}%", Gray));
            Thread.Sleep(11);
        }
        Thread.Sleep(120);
        ShowCursor();
    }

    // ---- Medida de largura visivel (ignora ANSI) ----
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

    // ---- Painel arredondado com titulo ----
    public static int Margin => Math.Max(2, (WinW - 74) / 2);
    private const int Inner = 70; // largura interna do painel

    public static void PanelTop(string title)
    {
        string m = new(' ', Margin);
        string t = " " + title + " ";
        string left = "╭─";
        int rest = Inner - 1 - t.Length;
        if (rest < 0) rest = 0;
        Console.WriteLine(m + Fg(Border) + left + Reset + B(t, Red) + Fg(Border) + new string('─', rest) + "╮" + Reset);
    }

    public static void PanelLine(string content)
    {
        string m = new(' ', Margin);
        int padLen = Inner - VisLen(content);
        if (padLen < 0) padLen = 0;
        Console.WriteLine(m + Fg(Border) + "│ " + Reset + content + new string(' ', padLen) + Fg(Border) + "│" + Reset);
    }

    public static void PanelBlank() => PanelLine("");

    public static void PanelBottom()
    {
        string m = new(' ', Margin);
        Console.WriteLine(m + Fg(Border) + "╰" + new string('─', Inner + 1) + "╯" + Reset);
    }

    public static void Gap() => Console.WriteLine();

    // ---- Badge de status: bolinha colorida + texto ----
    public static string Dot((int r, int g, int b) c, string text) => C("•", c) + " " + C(text, White);

    // ---- Linha de label + valor dentro do painel ----
    public static string Field(string label, string valueColored)
        => C(label.PadRight(10), DimC) + valueColored;

    // ---- Step animado (sem borda; usa \r para reescrever) ----
    public static bool Step(string label, Func<bool> work)
    {
        string m = new(' ', Margin);
        Console.Write(m + "  " + C("◌", DimC) + " " + C(label + "...", Gray));
        bool ok; try { ok = work(); } catch { ok = false; }
        Thread.Sleep(70);
        string mark = ok ? C("✔", Green) : C("✖", Red);
        Console.Write("\r" + m + "  " + mark + " " + C(label, ok ? White : Red) + new string(' ', 16) + "\n");
        return ok;
    }

    // ---- Prompt de escolha ----
    public static void Prompt(string text)
    {
        ShowCursor();
        string m = new(' ', Margin);
        Console.Write("\n" + m + Fg(Red) + "▶ " + Reset + B(text, White) + " ");
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
}
