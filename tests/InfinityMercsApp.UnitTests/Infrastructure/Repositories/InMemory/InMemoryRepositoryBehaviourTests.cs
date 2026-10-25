using System.Linq.Expressions;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories.InMemory;

/// <summary>What the in-memory repository does on its own, beyond answering the providers as SQLite does.</summary>
public sealed class InMemoryRepositoryBehaviourTests
{
    private readonly InMemoryRepository _repository = new();

    // row => row.Name.Method(argument), with no null check of its own: the repository must add SQLite's.
    private static Expression<Func<CountedRow, bool>> NameCalls(string method, string argument)
    {
        var row = Expression.Parameter(typeof(CountedRow), "row");
        var call = Expression.Call(
            Expression.Property(row, nameof(CountedRow.Name)),
            typeof(string).GetMethod(method, [typeof(string)]) ?? throw new MissingMethodException(nameof(String), method),
            Expression.Constant(argument));
        return Expression.Lambda<Func<CountedRow, bool>>(call, row);
    }

    private void Add(params IEnumerable<CountedRow> rows) => _repository.Insert(rows);

    [Fact]
    public void Insert_AutoIncrementKey_NumbersRowsAfterTheHighestKey()
    {
        Add(new CountedRow { Id = 5 }, new CountedRow(), new CountedRow());

        Assert.Equal([5, 6, 7], _repository.GetAll<CountedRow>(_ => true).Select(row => row.Id));
    }

    [Fact]
    public void Insert_DuplicateKey_Throws()
    {
        Add(new CountedRow { Id = 1 });

        Assert.Throws<InvalidOperationException>(() => Add(new CountedRow { Id = 1 }));
    }

    [Fact]
    public void GetAll_IntegerKeysAddedOutOfOrder_ComesBackByKey()
    {
        Add(new CountedRow { Id = 3 }, new CountedRow { Id = 1 }, new CountedRow { Id = 2 });

        Assert.Equal([1, 2, 3], _repository.GetAll<CountedRow>(_ => true).Select(row => row.Id));
        Assert.Equal(2, _repository.GetById<CountedRow>(2)?.Id);
    }

    [Fact]
    public void GetAll_TextKeys_ComesBackInTheOrderAdded()
    {
        _repository.Insert([new CodedRow { Code = "b" }, new CodedRow { Code = "a" }]);

        Assert.Equal(["b", "a"], _repository.GetAll<CodedRow>(_ => true).Select(row => row.Code));
    }

    [Fact]
    public void GetAll_OrderByText_OrdersByCharacterCode()
    {
        Add(new CountedRow { Name = "b" }, new CountedRow { Name = "B" }, new CountedRow { Name = "a" });

        Assert.Equal(["B", "a", "b"], _repository.GetAll<CountedRow>(_ => true, row => row.Name ?? string.Empty).Select(row => row.Name));
    }

    [Fact]
    public void GetAll_Limit_TakesThatMany()
    {
        Add(new CountedRow { Rank = 3 }, new CountedRow { Rank = 1 }, new CountedRow { Rank = 2 });

        Assert.Equal([1, 2], _repository.GetAll<CountedRow>(_ => true, row => row.Rank, 2).Select(row => row.Rank));
    }

    [Fact]
    public void GetById_Missing_IsNull() => Assert.Null(_repository.GetById<CountedRow>(9));

    [Fact]
    public void FirstOrDefault_NoMatch_IsNull()
    {
        Add(new CountedRow { Rank = 1 });

        Assert.Null(_repository.FirstOrDefault<CountedRow>(row => row.Rank == 2));
    }

    [Fact]
    public void GetAll_RowChangedAfterReading_LeavesTheStoredRow()
    {
        Add(new CountedRow { Name = "kept" });

        var read = _repository.GetAll<CountedRow>(_ => true).Single();
        read.Name = "changed";

        Assert.Equal("kept", _repository.GetById<CountedRow>(read.Id)?.Name);
    }

    [Fact]
    public void Update_KnownKey_ReplacesTheRow()
    {
        Add(new CountedRow { Name = "old" });
        var row = _repository.GetById<CountedRow>(1) ?? throw new InvalidOperationException("The row wasn't added.");
        row.Name = "new";

        _repository.Update(row);

        Assert.Equal("new", _repository.GetById<CountedRow>(1)?.Name);
    }

    [Fact]
    public void Update_UnknownKey_ChangesNothing()
    {
        Add(new CountedRow { Name = "old" });

        _repository.Update(new CountedRow { Id = 7, Name = "new" });

        Assert.Equal(["old"], _repository.GetAll<CountedRow>(_ => true).Select(row => row.Name));
    }

    [Fact]
    public void Update_NoPrimaryKey_Throws()
    {
        _repository.Insert([new KeylessRow { Name = "a" }]);

        Assert.Throws<InvalidOperationException>(() => _repository.Update(new KeylessRow { Name = "b" }));
    }

    [Fact]
    public void CountExistsAny_Rows_AnswerFromTheFilter()
    {
        Assert.False(_repository.Any<CountedRow>());
        Add(new CountedRow { Rank = 1 }, new CountedRow { Rank = 2 }, new CountedRow { Rank = 2 });

        Assert.True(_repository.Any<CountedRow>());
        Assert.Equal(2, _repository.Count<CountedRow>(row => row.Rank == 2));
        Assert.True(_repository.Exists<CountedRow>(row => row.Rank == 1));
        Assert.False(_repository.Exists<CountedRow>(row => row.Rank == 3));
    }

    [Fact]
    public void Delete_Filter_RemovesMatchesAndKeepsKeyLookups()
    {
        Add(new CountedRow { Rank = 1 }, new CountedRow { Rank = 2 }, new CountedRow { Rank = 3 });

        _repository.Delete<CountedRow>(row => row.Rank == 1);

        Assert.Null(_repository.GetById<CountedRow>(1));
        Assert.Equal(3, _repository.GetById<CountedRow>(3)?.Rank);
    }

    [Fact]
    public void DeleteAll_Rows_LeavesNone()
    {
        Add(new CountedRow(), new CountedRow());

        _repository.DeleteAll<CountedRow>();

        Assert.False(_repository.Any<CountedRow>());
    }

    [Fact]
    public void QueryColumn_MappedColumnName_ReadsThatProperty()
    {
        Add(new CountedRow { Name = "a" }, new CountedRow { Name = "b" });

        Assert.Equal(["a", "b"], _repository.QueryColumn<CountedRow, string>("display_name"));
    }

    [Fact]
    public void QueryColumn_OtherType_ConvertsTheValue()
    {
        Add(new CountedRow { Rank = 4 });

        Assert.Equal([4L], _repository.QueryColumn<CountedRow, long>("rank"));
    }

    [Fact]
    public void QueryColumn_NullValue_Throws()
    {
        Add(new CountedRow { Name = null });

        Assert.Throws<InvalidOperationException>(() => _repository.QueryColumn<CountedRow, string>("display_name"));
    }

    [Fact]
    public void QueryColumn_UnknownColumn_Throws() =>
        Assert.Throws<ArgumentException>(() => _repository.QueryColumn<CountedRow, string>("missing"));

    [Fact]
    public void RunInTransaction_Action_RunsIt()
    {
        _repository.RunInTransaction(() => Add(new CountedRow()));

        Assert.True(_repository.Any<CountedRow>());
    }

    [Fact]
    public void Contains_DifferentCase_DoesNotMatchAsInstrDoesNot()
    {
        Add(new CountedRow { Name = "Fusilier" }, new CountedRow { Name = null });

        Assert.Equal(0, _repository.Count<CountedRow>(row => row.Name != null && row.Name.Contains("fus")));
        Assert.Equal(1, _repository.Count(NameCalls(nameof(string.Contains), "Fus")));
    }

    [Fact]
    public void StartsWithEndsWith_DifferentCase_MatchAsLikeDoes()
    {
        Add(new CountedRow { Name = "Fusilier" }, new CountedRow { Name = null });

        Assert.Equal(1, _repository.Count(NameCalls(nameof(string.StartsWith), "fus")));
        Assert.Equal(1, _repository.Count(NameCalls(nameof(string.EndsWith), "IER")));
    }

    [Fact]
    public void Count_OtherStringMethods_AreLeftAlone()
    {
        Add(new CountedRow { Name = "Fusilier" });

        Assert.Equal(1, _repository.Count<CountedRow>(row => row.Name != null && row.Name.Equals("Fusilier")));
    }
}
