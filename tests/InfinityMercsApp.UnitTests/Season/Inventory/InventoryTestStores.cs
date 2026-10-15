using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.UnitTests.Season.Inventory;

/// <summary>Two markets and a service, shared by the inventory tests.</summary>
internal static class InventoryTestStores
{
    public static Store NumberOne { get; } = new()
    {
        Name = "Number One",
        Items =
        [
            new StoreItem { Name = "Combi Rifle", Category = "Primary", CostCr = 12 },
            new StoreItem { Name = "Pistol", Category = "Sidearm", CostCr = 3 },
            new StoreItem { Name = "SWC +1", Category = "Exchange", CostCr = 20 },
            new StoreItem { Name = "Rations", CostCr = 1 }
        ],
        TroopTypes = [new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10 }],
        Augments = [new StoreAugment { Name = "WIP=12", CostCr = 15 }]
    };

    public static Store JadeTemu { get; } = new()
    {
        Name = "Jade Temu",
        Items = [new StoreItem { Name = "Pistol", Category = "Sidearm", CostCr = 5 }]
    };

    public static Store Medical { get; } = new()
    {
        Name = ServiceStoreNames.MedicalServices,
        Items = [new StoreItem { Name = "Surgery", Category = "Primary", CostCr = 8 }]
    };
}
