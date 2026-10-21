namespace InfinityMercsApp.Domain.Models.Army.Filters;

/// <summary>
/// A filter item whose id is text rather than a number, e.g. the <c>attrs</c> section's
/// <c>{"id":"cc","name":"CC","abbr":"CC","min":0,"max":30}</c> or a fractional <c>swc</c> id such as "0.5".
/// </summary>
public sealed record ArmyFilterKeyedItem(string Key, string? Name, string? Abbr, int? Min, int? Max);
