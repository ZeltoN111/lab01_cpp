using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class CatalogServiceTests
{
    [Fact] // 1) інваріант: відмова і незмінний стан
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Кабель", "м", 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive(p.Id, -5));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact] // 2) подія з тижня 6 як спосіб перевірки
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Fact] // варіант «Склад»: кількість 0 — бізнес-помилка, стан і події не змінюються
    public void Receive_WithZeroQuantity_ThrowsDomainException_AndRaisesNoEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-3", "Цемент", "шт", 4);
        var events = 0;
        service.Changed += (_, _) => events++;

        var ex = Assert.Throws<DomainException>(() => service.Receive(p.Id, 0));

        Assert.Contains("не може бути 0", ex.Message);
        Assert.Equal(4, store.GetById(p.Id)!.Quantity);
        Assert.Equal(0, events);
    }

    [Fact]
    public void Issue_MoreThanStock_ThrowsDomainException_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-4", "Пісок", "т", 2);

        Assert.Throws<DomainException>(() => service.Issue(p.Id, 3));
        Assert.Equal(2, store.GetById(p.Id)!.Quantity);
    }

    [Fact]
    public void Receive_UnknownId_ThrowsDomainException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        Assert.Throws<DomainException>(() => service.Receive("no-such-id", 1));
    }
}
