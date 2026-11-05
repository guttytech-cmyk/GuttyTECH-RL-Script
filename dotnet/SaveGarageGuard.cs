using System.Security.Cryptography;

namespace GuttyRL;

/// <summary>
/// Detecta save regravado pelo codec antigo. O assemble troca ObjectProperty
/// e ByteProperty por Int/Str: o preset perde o vinculo e a tinta cai na cor do time.
/// O header AES continua valido, entao tamanho e CRC nao revelam o estrago.
/// </summary>
internal static class SaveGarageGuard
{
    private static readonly byte[] AesKey =
    {
        0xD7, 0x8C, 0x32, 0x4A, 0x94, 0x42, 0x94, 0x3C,
        0x6D, 0x65, 0xCE, 0x98, 0x81, 0x85, 0x4C, 0x41,
        0x68, 0x99, 0x22, 0x0C, 0xC7, 0xA1, 0x46, 0x40,
        0x93, 0x9B, 0x96, 0x3C, 0x93, 0x2A, 0x6F, 0xAF,
    };

    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, (long Length, long Ticks, bool Damaged, int Loadouts)> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsReserializeDamage(string path) => Inspect(path).Damaged;

    /// <summary>Quantidade de objetos Loadout_TA (presets por time) no save.</summary>
    public static int LoadoutCount(string path) => Inspect(path).Loadouts;

    private static (bool Damaged, int Loadouts) Inspect(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length < 32)
                return (false, 0);

            long ticks = fi.LastWriteTimeUtc.Ticks;
            lock (Gate)
            {
                if (Cache.TryGetValue(path, out var hit)
                    && hit.Length == fi.Length
                    && hit.Ticks == ticks)
                    return (hit.Damaged, hit.Loadouts);
            }

            var r = InspectFile(fi.FullName);
            lock (Gate)
                Cache[path] = (fi.Length, ticks, r.Damaged, r.Loadouts);
            return r;
        }
        catch
        {
            return (false, 0);
        }
    }

    internal static bool LooksLikeReserialize(ReadOnlySpan<byte> plain)
    {
        int products = Count(plain, "TAGame.OnlineProduct_TA"u8);
        int loadouts = Count(plain, "TAGame.LoadoutSet_TA"u8);
        int painted = Count(plain, "TAGame.ProductAttribute_Painted_TA"u8);
        int objectProps = Count(plain, "ObjectProperty"u8);
        int byteProps = Count(plain, "ByteProperty"u8);

        bool hasGarage = products > 0 || loadouts > 0 || painted > 0;
        if (!hasGarage)
            return false;

        // O repair troca TeamColorID/CustomColorID Int→Byte e LowerBits/UpperBits Int→QWord.
        // Nao recria ObjectProperty. Tratar objectProps==0 como dano depois do repair
        // faz o Heal copiar outro arquivo por cima do save que acabou de ser consertado.
        bool sawColor = false, colorOk = true;
        bool sawId = false, idOk = true;
        ScanType(plain, "TeamColorID"u8, TypeKind.Byte, ref sawColor, ref colorOk);
        ScanType(plain, "CustomColorID"u8, TypeKind.Byte, ref sawColor, ref colorOk);
        ScanType(plain, "LowerBits"u8, TypeKind.QWord, ref sawId, ref idOk);
        ScanType(plain, "UpperBits"u8, TypeKind.QWord, ref sawId, ref idOk);
        if (!colorOk || !idOk)
            return true;
        if (sawColor && colorOk && sawId && idOk)
            return false;

        // Roundtrip do codec: os nomes das classes ficam, os tipos UE3 somem.
        // O arquivo medido ficou com ByteProperty=3 (nao zero) e ObjectProperty=0.
        if ((products > 0 || loadouts > 0) && objectProps == 0)
            return true;
        if (painted >= 8 && byteProps * 8 < painted)
            return true;
        return false;
    }

    private enum TypeKind { Int, Byte, QWord }

    /// <summary>
    /// O nome e o tipo nao ficam colados: no meio tem o tamanho da string (4 bytes).
    /// Olha so a janela curta depois do nome, e usa o tipo mais proximo.
    /// </summary>
    private static void ScanType(
        ReadOnlySpan<byte> plain,
        ReadOnlySpan<byte> name,
        TypeKind good,
        ref bool saw,
        ref bool allGood)
    {
        int from = 0;
        while (from <= plain.Length - name.Length)
        {
            int found = plain[from..].IndexOf(name);
            if (found < 0)
                break;
            int start = from + found + name.Length;
            int end = Math.Min(plain.Length, start + 40);
            TypeKind? kind = NearestKind(plain[start..end]);
            if (kind is not null)
            {
                saw = true;
                if (kind != good)
                    allGood = false;
            }
            from = start;
        }
    }

    private static TypeKind? NearestKind(ReadOnlySpan<byte> window)
    {
        int iInt = window.IndexOf("IntProperty"u8);
        int iByte = window.IndexOf("ByteProperty"u8);
        int iQ = window.IndexOf("QWordProperty"u8);
        int best = int.MaxValue;
        TypeKind? kind = null;
        if (iInt >= 0 && iInt < best) { best = iInt; kind = TypeKind.Int; }
        if (iByte >= 0 && iByte < best) { best = iByte; kind = TypeKind.Byte; }
        if (iQ >= 0 && iQ < best) kind = TypeKind.QWord;
        return kind;
    }

    private static (bool Damaged, int Loadouts) InspectFile(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 8)
            return (false, 0);

        int encLen = BitConverter.ToInt32(file, 0);
        if (encLen <= 0 || encLen % 16 != 0 || 8 + encLen > file.Length)
            return (false, 0);

        byte[] plain = DecryptEcb(file.AsSpan(8, encLen));
        return (LooksLikeReserialize(plain), Count(plain, "TAGame.Loadout_TA"u8));
    }

    private static byte[] DecryptEcb(ReadOnlySpan<byte> cipher)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = AesKey;
        using ICryptoTransform dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher.ToArray(), 0, cipher.Length);
    }

    private static int Count(ReadOnlySpan<byte> hay, ReadOnlySpan<byte> needle)
    {
        int n = 0;
        int from = 0;
        while (from <= hay.Length - needle.Length)
        {
            int found = hay[from..].IndexOf(needle);
            if (found < 0)
                break;
            n++;
            from += found + needle.Length;
        }
        return n;
    }
}
