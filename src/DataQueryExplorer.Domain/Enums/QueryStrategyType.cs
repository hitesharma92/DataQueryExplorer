namespace DataQueryExplorer.Domain.Enums;

public enum QueryStrategyType
{
    SingleContainerQuery = 1,
    TwoLevelJoinAllResults = 2,
    TwoLevelJoinOrphansOnly = 3,
    TwoLevelJoinFindDuplicates = 4,
    ThreeLevelJoinAllResults = 5,
    ThreeLevelJoinInnerMatchOnly = 6
}
