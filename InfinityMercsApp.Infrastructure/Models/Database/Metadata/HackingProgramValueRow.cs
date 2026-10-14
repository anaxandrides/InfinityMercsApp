namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

/// <summary>
/// One value of a hacking program's lists: <see cref="ListKind"/> is <c>skillType</c> or <c>target</c>
/// (text in <see cref="Text"/>) or <c>devices</c> (an id in <see cref="Number"/>).
/// </summary>
[Table("hacking_program_values")]
public class HackingProgramValueRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int ProgramId { get; set; }

    public string ListKind { get; set; } = string.Empty;

    public int Position { get; set; }

    public string? Text { get; set; }

    public int? Number { get; set; }
}
