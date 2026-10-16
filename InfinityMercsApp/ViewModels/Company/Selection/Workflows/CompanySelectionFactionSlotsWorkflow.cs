using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

internal static class CompanySelectionFactionSlotsWorkflow
{
    internal static bool IsDuplicateSelectionForActiveSlot(
        bool showRightSelectionBox,
        int activeSlotIndex,
        ArmyFactionSelectionItem? leftSlotFaction,
        ArmyFactionSelectionItem? rightSlotFaction,
        ArmyFactionSelectionItem item)
    {
        if (!showRightSelectionBox)
        {
            return false;
        }

        if (activeSlotIndex == 0)
        {
            return rightSlotFaction is not null
                && rightSlotFaction.Id == item.Id
                && (leftSlotFaction is null || leftSlotFaction.Id != item.Id);
        }

        return leftSlotFaction is not null
            && leftSlotFaction.Id == item.Id
            && (rightSlotFaction is null || rightSlotFaction.Id != item.Id);
    }

    internal static int ResolveActiveSlotIndex(int index, bool showRightSelectionBox)
    {
        return index == 1 && showRightSelectionBox ? 1 : 0;
    }

    internal static int ResolveAutoSelectedSlotIndex<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        int currentActiveSlotIndex)
        where TFaction : class
    {
        if (!showRightSelectionBox)
        {
            return 0;
        }

        var leftEmpty = leftSlotFaction is null;
        var rightEmpty = rightSlotFaction is null;

        if (leftEmpty && !rightEmpty)
        {
            return 0;
        }

        if (rightEmpty && !leftEmpty)
        {
            return 1;
        }

        return currentActiveSlotIndex;
    }
}
