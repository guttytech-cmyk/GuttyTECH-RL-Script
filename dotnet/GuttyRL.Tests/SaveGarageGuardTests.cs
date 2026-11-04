using System.Security.Cryptography;
using Xunit;
using System.Text;

namespace GuttyRL.Tests;

public class SaveGarageGuardTests
{
    private static readonly byte[] AesKey =
    {
        0xD7, 0x8C, 0x32, 0x4A, 0x94, 0x42, 0x94, 0x3C,
        0x6D, 0x65, 0xCE, 0x98, 0x81, 0x85, 0x4C, 0x41,
        0x68, 0x99, 0x22, 0x0C, 0xC7, 0xA1, 0x46, 0x40,
        0x93, 0x9B, 0x96, 0x3C, 0x93, 0x2A, 0x6F, 0xAF,
    };

    [Fact]
    public void Save_integro_com_vinculo_e_tinta_passa()
    {
        byte[] plain = Encoding.ASCII.GetBytes(
            "TAGame.OnlineProduct_TA TAGame.LoadoutSet_TA ObjectProperty " +
            "ByteProperty ByteProperty ByteProperty " +
            "TAGame.ProductAttribute_Painted_TA TAGame.ProductAttribute_Painted_TA TAGame.ProductAttribute_Painted_TA");
        Assert.False(SaveGarageGuard.LooksLikeReserialize(plain));
    }

    [Fact]
    public void Roundtrip_sem_object_property_e_dano()
    {
        byte[] plain = Encoding.ASCII.GetBytes(
            "TAGame.OnlineProduct_TA TAGame.LoadoutSet_TA " +
            "TAGame.ProductAttribute_Painted_TA TAGame.ProductAttribute_Painted_TA TAGame.ProductAttribute_Painted_TA");
        Assert.True(SaveGarageGuard.LooksLikeReserialize(plain));
    }

    [Fact]
    public void Tinta_perdida_com_tres_byte_property_e_dano()
    {
        // Roundtrip real: ObjectProperty some e sobram 3 ByteProperty para centenas de pinturas.
        // Aqui ObjectProperty existe de proposito, para o outro criterio nao mascarar este.
        var sb = new StringBuilder("TAGame.LoadoutSet_TA ObjectProperty ByteProperty ByteProperty ByteProperty ");
        for (int i = 0; i < 40; i++)
            sb.Append("TAGame.ProductAttribute_Painted_TA ");
        Assert.True(SaveGarageGuard.LooksLikeReserialize(Encoding.ASCII.GetBytes(sb.ToString())));
    }

    [Fact]
    public void Stub_de_video_nao_e_garagem_quebrada()
    {
        byte[] plain = Encoding.ASCII.GetBytes("TAGame.VideoSettingsSavePC_TA StrProperty MaxFPS");
        Assert.False(SaveGarageGuard.LooksLikeReserialize(plain));
    }

    [Fact]
    public void Arquivo_cifrado_quebrado_e_detectado()
    {
        string path = Path.Combine(Path.GetTempPath(), "gutty-guard-" + Guid.NewGuid().ToString("N") + ".save");
        try
        {
            byte[] plain = Encoding.ASCII.GetBytes("TAGame.OnlineProduct_TA TAGame.LoadoutSet_TA");
            byte[] padded = Pad16(plain);
            byte[] enc = Encrypt(padded);
            using (var fs = File.Create(path))
            {
                fs.Write(BitConverter.GetBytes(enc.Length));
                fs.Write(BitConverter.GetBytes(0));
                fs.Write(enc);
            }

            Assert.True(SaveGarageGuard.IsReserializeDamage(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void Conta_loadouts_sem_confundir_com_loadoutset()
    {
        string path = Path.Combine(Path.GetTempPath(), "gutty-guard-" + Guid.NewGuid().ToString("N") + ".save");
        try
        {
            byte[] plain = Encoding.ASCII.GetBytes(
                "TAGame.LoadoutSet_TA TAGame.Loadout_TA TAGame.Loadout_TA TAGame.Loadout_TA ObjectProperty");
            byte[] enc = Encrypt(Pad16(plain));
            using (var fs = File.Create(path))
            {
                fs.Write(BitConverter.GetBytes(enc.Length));
                fs.Write(BitConverter.GetBytes(0));
                fs.Write(enc);
            }

            Assert.Equal(3, SaveGarageGuard.LoadoutCount(path));
            Assert.False(SaveGarageGuard.IsReserializeDamage(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    private static byte[] Pad16(byte[] data)
    {
        int len = (data.Length + 15) & ~15;
        var buf = new byte[len];
        data.CopyTo(buf, 0);
        return buf;
    }

    private static byte[] Encrypt(byte[] plain)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = AesKey;
        using ICryptoTransform enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }
}
