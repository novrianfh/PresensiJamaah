using PresensiJamaah.Application.Models;

namespace PresensiJamaah.Application.Abstractions;

/// <summary>Membuat file Excel (.xlsx) dari hasil rekap.</summary>
public interface IRekapExcel
{
    byte[] Buat(RekapResult rekap);
}
