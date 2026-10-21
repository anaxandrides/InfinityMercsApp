using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Presentation.Sync;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.UnitTests.Presentation.Sync;

public sealed class SyncConflictQuestionTests
{
    private static readonly SyncConflict Changed = new()
    {
        Kind = UserDocumentKind.Company,
        DocumentId = "c1",
        Name = "Iron Wolves",
        OtherUpdatedAt = new DateTimeOffset(2026, 10, 21, 18, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public void For_ChangedOnBoth_AsksWhichVersionToKeep()
    {
        var question = SyncConflictQuestion.For(Changed);

        Assert.StartsWith("The company \"Iron Wolves\" was changed on this device and on another", question.Question);
        Assert.Equal(SyncChoice.KeepThisDevice, question.ChoiceFor("This Device's"));
        Assert.Equal(SyncChoice.UseOtherDevice, question.ChoiceFor("The Other Device's"));
    }

    [Fact]
    public void For_DeletedElsewhere_KeepingItKeepsThisDevicesVersion()
    {
        var question = SyncConflictQuestion.For(Changed with { Kind = UserDocumentKind.Season, OtherDeleted = true });

        Assert.StartsWith("The season of \"Iron Wolves\" was deleted on another device", question.Question);
        Assert.Equal(SyncChoice.KeepThisDevice, question.ChoiceFor("Keep It"));
        Assert.Equal(SyncChoice.UseOtherDevice, question.ChoiceFor("Delete It"));
    }

    [Fact]
    public void For_DeletedHere_DeletingItKeepsThisDevicesChoice()
    {
        var question = SyncConflictQuestion.For(Changed with { DeletedHere = true });

        Assert.Equal(SyncChoice.KeepThisDevice, question.ChoiceFor("Delete It"));
        Assert.Equal(SyncChoice.UseOtherDevice, question.ChoiceFor("Keep the Other Device's"));
    }

    [Fact]
    public void ChoiceFor_DecideLater_IsNull()
    {
        Assert.Null(SyncConflictQuestion.For(Changed).ChoiceFor(SyncConflictQuestion.DecideLater));
        Assert.Null(SyncConflictQuestion.For(Changed).ChoiceFor(null));
    }
}
