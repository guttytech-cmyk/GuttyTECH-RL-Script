namespace GuttyRL;

/// <summary>Tela de conclusao premium (Awwwards): faixa de titulo, steps animados,
/// painel CONCLUIDO com glow/gradiente, checklist, botao ENTER. Console puro, true-color.</summary>
internal static partial class Ui
{
    // ---- Paleta da tela de conclusao (prompt UI Polish v22) ----
    public static readonly (int r, int g, int b) MRed = (255, 42, 42);
    public static readonly (int r, int g, int b) MCyan = (0, 229, 255);
    public static readonly (int r, int g, int b) MAmber = (255, 179, 0);
    public static readonly (int r, int g, int b) OkGreen = (0, 255, 65);
    public static readonly (int r, int g, int b) WarnYel = (255, 215, 0);
    public static readonly (int r, int g, int b) LightGray = (192, 192, 192);
    public static readonly (int r, int g, int b) DarkGray = (102, 102, 102);
    private static readonly (int r, int g, int b) PanelTopBg = (24, 24, 24);
    private static readonly (int r, int g, int b) PanelBotBg = (12, 12, 12);
    private static readonly (int r, int g, int b) BtnBg = (42, 42, 42);
    private static readonly (int r, int g, int b) BtnBorder = (85, 85, 85);
    private static readonly (int r, int g, int b) SpinGray = (130, 130, 130);

    private static readonly string[] Spin = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

    private const int CW = 60;                 // largura interna dos paineis de conclusao
    private static string CMar => new(' ', Math.Max(2, (WinW - CW - 2) / 2));

    public static (int r, int g, int b) ModeColor(string m) => m == "COMPLETO" ? MRed : m == "CRIADOR" ? MCyan : MAmber;

    public static void FlushInput() { try { while (Console.KeyAvailable) Console.ReadKey(true); } catch { } }

    // ---- builders de conteudo (SEM Reset, pra preservar o fundo da linha) ----
    private static string Raw(string t, (int r, int g, int b) c) => Fg(c) + t;
    private static string RawB(string t, (int r, int g, int b) c) => "\x1b[1m" + Fg(c) + t + "\x1b[22m";
    private static string CenterRaw(string plain, (int r, int g, int b) c, bool bold = false)
    {
        int left = Math.Max(0, (CW - plain.Length) / 2);
        string s = new string(' ', left) + plain;
        return bold ? RawB(s, c) : Raw(s, c);
    }
    private static string Trunc(string s, int max) => s.Length <= max ? s : "..." + s[^(max - 3)..];

    // ---- mini banner opcional (so se a janela for alta) ----
    public static void MiniBannerIfTall((int r, int g, int b) acc)
    {
        if (WinH < 36) return;
        string txt = "G U T T Y T E C H   -   R O C K E T   L E A G U E";
        int w = txt.Length + 4;
        string m = new(' ', Math.Max(2, (WinW - w) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(acc) + "╔" + new string('═', w - 2) + "╗" + Reset);
        Console.WriteLine(m + Fg(acc) + "║ " + RawB(txt, White) + Fg(acc) + " ║" + Reset);
        Console.WriteLine(m + Fg(acc) + "╚" + new string('═', w - 2) + "╝" + Reset);
    }

    // ---- faixa de titulo (glow no fundo, texto branco bold) ----
    public static void TitleBar(string text, (int r, int g, int b) acc)
    {
        string m = CMar;
        var glow = Lerp(acc, (10, 10, 10), 0.80);
        Console.WriteLine();
        Console.WriteLine(m + Fg(acc) + "╭" + new string('─', CW) + "╮" + Reset);
        string body = "  " + text;
        int pad = CW - body.Length; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(acc) + "│" + Bg(glow) + "\x1b[1m" + Fg(White) + body + new string(' ', pad) + Reset + Fg(acc) + "│" + Reset);
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', CW) + "╯" + Reset);
        Console.WriteLine();
    }

    // ---- step com spinner Braille animado -> vira ✓ (verde) ou ✗ (vermelho) ----
    public static bool StepAnimated(string label, Func<bool> work)
    {
        string m = CMar;
        Console.Write(m + "  " + Fg(SpinGray) + Spin[0] + Reset + " " + Fg(LightGray) + label + "..." + Reset);
        bool ok; try { ok = work(); } catch { ok = false; }
        for (int i = 1; i < 9; i++)
        {
            Console.Write("\r" + m + "  " + Fg(SpinGray) + Spin[i % Spin.Length] + Reset + " " + Fg(LightGray) + label + "..." + Reset);
            Thread.Sleep(34);
        }
        string mark = ok ? Fg(OkGreen) + "✓" : Fg(MRed) + "✗";
        Console.WriteLine("\r" + m + "  " + mark + Reset + " " + Fg(White) + label + Reset + new string(' ', 16));
        Thread.Sleep(110);
        return ok;
    }

    // ---- paineis (borda na cor do modo, fundo em gradiente vertical) ----
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
            if (reveal) Thread.Sleep(36);
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
            if (reveal) Thread.Sleep(36);
        }
        Console.WriteLine(m + Fg(acc) + "╰" + new string('─', CW) + "╯" + Reset);
    }

    // ---- painel CONCLUIDO ----
    public static void CompletionSuccess(string mode, (int r, int g, int b) acc, string backupPath)
    {
        var c = new List<string>
        {
            "",
            CenterRaw("✓  CONCLUIDO  ✓", acc, true),
            CenterRaw("══════════════", acc),
            "",
            "  " + Raw("» ", acc) + Raw("MODO " + mode + " aplicado com ", White) + RawB("sucesso", OkGreen) + Raw("!", White),
            "",
            "  " + Raw(mode == "CRIADOR"
                ? "Graficos ajustaveis no jogo; otimizacao do criador mantida."
                : "Menu de video sincronizado. Abra o RL — sem tutorial.", LightGray),
            "  " + Raw("Backups: " + Trunc(backupPath, CW - 13), DarkGray),
            ""
        };
        DrawPanel(acc, c, reveal: true);
    }

    // ---- painel de conclusao simples (REMOVER, falhas) ----
    public static void CompletionMessage((int r, int g, int b) acc, string title, string[] lines)
    {
        var c = new List<string>
        {
            "",
            CenterRaw("✓  " + title + "  ✓", acc, true),
            CenterRaw(new string('═', Math.Min(CW - 4, title.Length + 8)), acc),
            ""
        };
        foreach (var ln in lines) c.Add("  " + Raw(ln, LightGray));
        c.Add("");
        DrawPanel(acc, c, reveal: true);
    }

    // ---- botao ENTER ----
    public static void EnterButton()
    {
        FlushInput();
        const int bw = 34;
        string m = new(' ', Math.Max(2, (WinW - bw - 2) / 2));
        Console.WriteLine();
        Console.WriteLine(m + Fg(BtnBorder) + "┌" + new string('─', bw) + "┐" + Reset);
        const string plain = "  pressione ENTER para continuar  ";
        int pad = bw - plain.Length; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(BtnBorder) + "│" + Bg(BtnBg) + Fg(White) + "  pressione " + RawB("ENTER", WarnYel) + Fg(White) + " para continuar  " + new string(' ', pad) + Reset + Fg(BtnBorder) + "│" + Reset);
        Console.WriteLine(m + Fg(BtnBorder) + "└" + new string('─', bw) + "┘" + Reset);
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

    // ---- Launch Options: caixa de comando estilo terminal (verde no escuro) ----
    public static void CodeBox(string text)
    {
        string m = CMar;
        Console.WriteLine(m + Fg(DarkGray) + "┌" + new string('─', CW) + "┐" + Reset);
        int pad = CW - text.Length - 2; if (pad < 0) pad = 0;
        Console.WriteLine(m + Fg(DarkGray) + "│" + Bg((26, 26, 26)) + " \x1b[1m" + Fg(OkGreen) + text + "\x1b[22m" + new string(' ', pad) + " " + Reset + Fg(DarkGray) + "│" + Reset);
        Console.WriteLine(m + Fg(DarkGray) + "└" + new string('─', CW) + "┘" + Reset);
    }

    public static void CopyStatus(bool ok)
    {
        string m = CMar;
        if (ok)
            Console.WriteLine(m + "  " + Fg(OkGreen) + "\x1b[1m> Copiado para a area de transferencia!\x1b[22m" + Reset
                + Fg(LightGray) + "  Cole com " + Reset + Fg(White) + "Ctrl+V" + Reset + Fg(LightGray) + " no launcher." + Reset);
        else
            Console.WriteLine(m + "  " + Fg(MAmber) + "! Nao copiou sozinho" + Reset + Fg(LightGray) + " — selecione o comando abaixo e Ctrl+C." + Reset);
    }

    public static void LaunchHeading(string text)
    {
        Console.WriteLine();
        Console.WriteLine(CMar + "\x1b[1m" + Fg(LightGray) + text + "\x1b[22m" + Reset);
    }

    public static void LaunchParam(string sym, (int r, int g, int b) color, string label, string desc)
    {
        Console.WriteLine(CMar + "  " + Fg(color) + sym + Reset + " " + Fg(White) + label.PadRight(22) + Reset + Fg(DarkGray) + desc + Reset);
    }

    public static void LaunchNote(string text)
    {
        Console.WriteLine();
        Console.WriteLine(CMar + Fg(DarkGray) + text + Reset);
    }

    public static void StepsPanel(string title, string[] steps, (int r, int g, int b) acc)
    {
        var c = new List<string> { "" };
        foreach (var s in steps) c.Add("  " + Raw(s, LightGray));
        c.Add("");
        DrawPanelTitled(acc, title, c, reveal: false);
    }
}
