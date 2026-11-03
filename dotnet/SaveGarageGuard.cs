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
    private static readonly Dictionary<string, (long Length, long Ticks, bool Damaged)> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsReserializeDamage(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length < 32)
                return false;

            long ticks = fi.LastWriteTimeUtc.Ticks;
            lock (Gate)
            {
                if (Cache.TryGetValue(path, out var hit)
                    && hit.Length == fi.Length
                    && hit.Ticks == ticks)
                    return hit.Damaged;
            }

            bool damaged = LooksDamagedFile(fi.FullName);
            lock (Gate)
                Cache[path] = (fi.Length, ticks, damaged);
            return damaged;
        }
        catch
        {
            return false;
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

        // Roundtrip do codec: os nomes das classes ficam, os tipos UE3 somem.
        // O arquivo medido ficou com ByteProperty=3 (nao zero) e ObjectProperty=0.
        if ((products > 0 || loadouts > 0) && objectProps == 0)
            return true;
        if (painted >= 8 && byteProps * 8 < painted)
            return true;
        return false;
    }

    private static bool LooksDamagedFile(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 8)
            return false;

        int encLen = BitConverter.ToInt32(file, 0);
        if (encLen <= 0 || encLen % 16 != 0 || 8 + encLen > file.Length)
            return false;

        byte[] plain = DecryptEcb(file.AsSpan(8, encLen));
        return LooksLikeReserialize(plain);
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
