using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Services;

/// <summary>
/// Impor jamaah dari Excel. Aturan:
/// - Semua baris divalidasi lebih dulu; bila ada yang salah, tidak ada data yang disimpan (all-or-nothing).
/// - Baris duplikat (nama sama + nomor HP sama, baik dengan data yang sudah ada maupun di dalam file) dilewati, bukan dianggap salah,
///   sehingga file yang sudah diperbaiki aman diunggah ulang.
/// - Semua jamaah baru disimpan dalam satu transaksi.
/// </summary>
public class JamaahImportService(IJamaahRepository repository, IJamaahExcel excel, TimeProvider timeProvider)
{
    public const int MaxBaris = 1000;

    public byte[] BuatTemplate() => excel.BuatTemplate();

    /// <exception cref="ImportFileException">File tidak valid atau header tidak sesuai template.</exception>
    public Task<JamaahImportResult> ImportAsync(Stream stream, CancellationToken ct = default)
    {
        var rows = excel.Baca(stream, MaxBaris);
        return ImportRowsAsync(rows, ct);
    }

    public async Task<JamaahImportResult> ImportRowsAsync(IReadOnlyList<JamaahImportRow> rows, CancellationToken ct = default)
    {
        if (rows.Count == 0)
            return Gagal(0, "File tidak berisi data jamaah. Isi mulai baris 2 (baris 1 adalah header).");
        if (rows.Count > MaxBaris)
            return Gagal(0, $"Jumlah baris melebihi batas {MaxBaris} per file. Pecah menjadi beberapa file.");

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var kesalahan = new List<ImportRowMessage>();
        var valid = new List<(int Baris, JamaahInput Input)>();

        foreach (var row in rows)
        {
            var errors = Validasi(row, today, out var input);
            if (errors.Count > 0)
                kesalahan.Add(new ImportRowMessage(row.BarisKe, string.Join("; ", errors)));
            else
                valid.Add((row.BarisKe, input!));
        }

        if (kesalahan.Count > 0)
            return new JamaahImportResult(0, [], kesalahan);

        var sudahAda = (await repository.ListNamaNomorHpAsync(ct))
            .Select(x => KunciDuplikat(x.Nama, x.NomorHp))
            .ToHashSet();

        var dilewati = new List<ImportRowMessage>();
        var diimpor = 0;

        foreach (var (baris, input) in valid)
        {
            var kunci = KunciDuplikat(input.Nama, input.NomorHp);
            if (!sudahAda.Add(kunci))
            {
                dilewati.Add(new ImportRowMessage(baris, $"{input.Nama} ({input.NomorHp}) sudah terdaftar atau muncul ganda di file."));
                continue;
            }

            var urutan = await repository.NextNomorUrutAsync(ct);
            var jamaah = Jamaah.Create(
                Jamaah.FormatNomorJamaah(urutan),
                input.Nama, input.NomorHp, input.Alamat, input.TanggalLahir, input.JenisKelamin);
            await repository.AddAsync(jamaah, ct);
            diimpor++;
        }

        if (diimpor > 0)
            await repository.SaveChangesAsync(ct);

        return new JamaahImportResult(diimpor, dilewati, []);
    }

    private static List<string> Validasi(JamaahImportRow row, DateOnly today, out JamaahInput? input)
    {
        var errors = new List<string>();
        input = null;

        var nama = row.Nama?.Trim();
        if (string.IsNullOrEmpty(nama)) errors.Add("Nama wajib diisi");
        else if (nama.Length > Jamaah.NamaMaxLength) errors.Add($"Nama maksimal {Jamaah.NamaMaxLength} karakter");

        var hp = Jamaah.NormalizeNomorHp(row.NomorHp);
        if (hp.Length == 0) errors.Add("Nomor HP wajib diisi");
        else if (!Jamaah.IsValidNomorHp(hp)) errors.Add($"Nomor HP '{row.NomorHp}' tidak valid (contoh: 081234567890)");

        var alamat = row.Alamat?.Trim();
        if (string.IsNullOrEmpty(alamat)) errors.Add("Alamat wajib diisi");
        else if (alamat.Length > Jamaah.AlamatMaxLength) errors.Add($"Alamat maksimal {Jamaah.AlamatMaxLength} karakter");

        if (row.TanggalLahir is null)
        {
            errors.Add(string.IsNullOrWhiteSpace(row.TanggalLahirTeks)
                ? "Tanggal lahir wajib diisi"
                : $"Tanggal lahir '{row.TanggalLahirTeks}' tidak terbaca (gunakan format dd/MM/yyyy)");
        }
        else if (row.TanggalLahir > today) errors.Add("Tanggal lahir tidak boleh di masa depan");
        else if (row.TanggalLahir.Value.Year < 1900) errors.Add("Tanggal lahir tidak valid");

        var jenisKelamin = ParseJenisKelamin(row.JenisKelaminTeks);
        if (jenisKelamin is null)
            errors.Add(string.IsNullOrWhiteSpace(row.JenisKelaminTeks)
                ? "Jenis kelamin wajib diisi"
                : $"Jenis kelamin '{row.JenisKelaminTeks}' tidak dikenal (isi Laki-laki atau Perempuan)");

        if (errors.Count == 0)
            input = new JamaahInput(nama!, hp, alamat!, row.TanggalLahir!.Value, jenisKelamin!.Value);

        return errors;
    }

    internal static JenisKelamin? ParseJenisKelamin(string? text)
    {
        var key = new string((text ?? string.Empty).Where(char.IsLetter).ToArray()).ToLowerInvariant();
        return key switch
        {
            "lakilaki" or "laki" or "l" or "pria" or "male" => JenisKelamin.LakiLaki,
            "perempuan" or "p" or "wanita" or "female" => JenisKelamin.Perempuan,
            _ => null
        };
    }

    /// <summary>Nama (tanpa beda huruf besar/kecil) + nomor HP yang disamakan awalannya (0 / 62 / +62).</summary>
    private static string KunciDuplikat(string nama, string nomorHp)
    {
        var hp = Jamaah.NormalizeNomorHp(nomorHp).TrimStart('+');
        if (hp.StartsWith('0')) hp = "62" + hp[1..];
        return $"{nama.Trim().ToLowerInvariant()}|{hp}";
    }

    private static JamaahImportResult Gagal(int baris, string pesan) =>
        new(0, [], [new ImportRowMessage(baris, pesan)]);
}
