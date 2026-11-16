using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Tests;

public class JamaahServiceTests
{
    private static JamaahInput Input(string nama = "Ahmad") =>
        new(nama, "081234567890", "Jl. Mawar 1", new DateOnly(1990, 5, 17), JenisKelamin.LakiLaki);

    [Fact]
    public async Task Create_MembuatNomorJamaahBerurutan_DanMenyimpan()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);

        var a = await service.CreateAsync(Input("Ahmad"));
        var b = await service.CreateAsync(Input("Budi"));

        Assert.Equal("J-000001", a.NomorJamaah);
        Assert.Equal("J-000002", b.NomorJamaah);
        Assert.True(a.IsActive);
        Assert.Equal(2, repo.Data.Count);
        Assert.Equal(2, repo.SaveCount);
    }

    [Fact]
    public async Task Update_MengubahDataTanpaMengubahNomorJamaah()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);
        var dibuat = await service.CreateAsync(Input("Ahmad"));

        var hasil = await service.UpdateAsync(dibuat.Id, Input("Ahmad Fauzi"));

        Assert.NotNull(hasil);
        Assert.Equal("Ahmad Fauzi", hasil.Nama);
        Assert.Equal(dibuat.NomorJamaah, hasil.NomorJamaah);
    }

    [Fact]
    public async Task Update_JamaahTidakAda_MengembalikanNull()
    {
        var service = new JamaahService(new FakeJamaahRepository());

        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), Input()));
    }

    [Fact]
    public async Task SetActive_MenonaktifkanDanMengaktifkanKembali()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);
        var dibuat = await service.CreateAsync(Input());

        Assert.True(await service.SetActiveAsync(dibuat.Id, false));
        Assert.False((await service.GetAsync(dibuat.Id))!.IsActive);

        Assert.True(await service.SetActiveAsync(dibuat.Id, true));
        Assert.True((await service.GetAsync(dibuat.Id))!.IsActive);
    }

    [Fact]
    public async Task SetActive_JamaahTidakAda_MengembalikanFalse()
    {
        var service = new JamaahService(new FakeJamaahRepository());

        Assert.False(await service.SetActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task Get_JamaahTidakAda_MengembalikanNull()
    {
        var service = new JamaahService(new FakeJamaahRepository());

        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_MenormalkanQuery()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);

        await service.ListAsync(new JamaahQuery("  ahmad  ", JamaahStatusFilter.Semua, Page: -3, PageSize: 5000));

        Assert.Equal("ahmad", repo.LastQuery!.Search);
        Assert.Equal(1, repo.LastQuery.Page);
        Assert.Equal(JamaahService.MaxPageSize, repo.LastQuery.PageSize);
    }

    [Fact]
    public async Task List_SearchKosong_MenjadiNull()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);

        await service.ListAsync(new JamaahQuery("   "));

        Assert.Null(repo.LastQuery!.Search);
    }

    [Fact]
    public async Task ListAllAktif_MengambilSemuaJamaahDenganFilterAktif()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahService(repo);
        await service.CreateAsync(Input("Ahmad"));
        await service.CreateAsync(Input("Budi"));

        var result = await service.ListAllAktifAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(JamaahStatusFilter.Aktif, repo.LastQuery!.Status);
    }

    [Fact]
    public async Task ListAllAktif_TanpaJamaah_MengembalikanDaftarKosong()
    {
        var service = new JamaahService(new FakeJamaahRepository());

        Assert.Empty(await service.ListAllAktifAsync());
    }
}
