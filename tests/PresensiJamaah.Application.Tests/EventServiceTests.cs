using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Tests;

public class EventServiceTests
{
    private static readonly DateTime Senin = new(2026, 11, 2, 18, 0, 0);

    private static EventInput Input(
        string nama = "Maghrib", Recurrence recurrence = Recurrence.None, DateTime? mulai = null) =>
        new(nama, "desk", mulai ?? Senin, (mulai ?? Senin).AddMinutes(30), recurrence, null);

    [Fact]
    public async Task Create_MenyimpanEvent()
    {
        var repo = new FakeEventRepository();
        var service = new EventService(repo);

        var dto = await service.CreateAsync(Input());

        Assert.Single(repo.Data);
        Assert.Equal(1, repo.SaveCount);
        Assert.Equal("Maghrib", dto.Nama);
        Assert.Equal(dto.Id, repo.Data[0].Id);
    }

    [Fact]
    public async Task Create_DataTidakValid_MelemparDanTidakMenyimpan()
    {
        var repo = new FakeEventRepository();
        var service = new EventService(repo);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Input(nama: " ")));

        Assert.Empty(repo.Data);
        Assert.Equal(0, repo.SaveCount);
    }

    [Fact]
    public async Task Update_Ada_MengubahData()
    {
        var repo = new FakeEventRepository();
        var service = new EventService(repo);
        var created = await service.CreateAsync(Input());

        var updated = await service.UpdateAsync(created.Id, Input("Isya", Recurrence.Harian));

        Assert.NotNull(updated);
        Assert.Equal("Isya", updated.Nama);
        Assert.Equal(Recurrence.Harian, updated.Recurrence);
    }

    [Fact]
    public async Task Update_TidakAda_MengembalikanNull()
    {
        var service = new EventService(new FakeEventRepository());

        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), Input()));
    }

    [Fact]
    public async Task Delete_Ada_MenghapusDanMenyimpan()
    {
        var repo = new FakeEventRepository();
        var service = new EventService(repo);
        var created = await service.CreateAsync(Input());

        Assert.True(await service.DeleteAsync(created.Id));

        Assert.Empty(repo.Data);
        Assert.Equal(2, repo.SaveCount);
    }

    [Fact]
    public async Task Delete_SudahPunyaPresensi_DitolakDanEventTetapAda()
    {
        var repo = new FakeEventRepository();
        var service = new EventService(repo);
        var created = await service.CreateAsync(Input());
        repo.EventDenganPresensi.Add(created.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(created.Id));

        Assert.Single(repo.Data);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task Delete_TidakAda_False()
    {
        var service = new EventService(new FakeEventRepository());

        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Get_TidakAda_Null()
    {
        var service = new EventService(new FakeEventRepository());

        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetOccurrences_GabungkanSemuaEvent_UrutWaktuLaluNama()
    {
        var service = new EventService(new FakeEventRepository());
        await service.CreateAsync(Input("Isya", Recurrence.Harian, new DateTime(2026, 11, 2, 19, 30, 0)));
        await service.CreateAsync(Input("Maghrib", Recurrence.Harian, new DateTime(2026, 11, 2, 18, 0, 0)));
        await service.CreateAsync(Input("Kajian Ahad", Recurrence.Mingguan, new DateTime(2026, 11, 1, 6, 0, 0)));

        var hasil = await service.GetOccurrencesAsync(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3));

        Assert.Equal(["Maghrib", "Isya", "Maghrib", "Isya"], hasil.Select(o => o.Nama));
        Assert.True(hasil.Zip(hasil.Skip(1)).All(p => p.First.Mulai <= p.Second.Mulai));
    }

    [Fact]
    public async Task GetOccurrences_RentangTerbalik_Melempar()
    {
        var service = new EventService(new FakeEventRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetOccurrencesAsync(new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public async Task GetOccurrences_RentangTerlaluPanjang_Melempar()
    {
        var service = new EventService(new FakeEventRepository());
        var from = new DateOnly(2026, 1, 1);

        // 366 hari inklusif masih boleh; 367 tidak.
        await service.GetOccurrencesAsync(from, from.AddDays(EventService.MaxRangeDays - 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetOccurrencesAsync(from, from.AddDays(EventService.MaxRangeDays)));
    }

    [Fact]
    public async Task GetOccurrences_SatuEvent_TidakAda_Null()
    {
        var service = new EventService(new FakeEventRepository());

        Assert.Null(await service.GetOccurrencesAsync(Guid.NewGuid(), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2)));
    }

    [Fact]
    public async Task GetOccurrences_SatuEvent_HanyaMilikEventItu()
    {
        var service = new EventService(new FakeEventRepository());
        var maghrib = await service.CreateAsync(Input("Maghrib", Recurrence.Harian));
        await service.CreateAsync(Input("Isya", Recurrence.Harian));

        var hasil = await service.GetOccurrencesAsync(maghrib.Id, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4));

        Assert.NotNull(hasil);
        Assert.Equal(3, hasil.Count);
        Assert.All(hasil, o => Assert.Equal(maghrib.Id, o.EventId));
    }
}
