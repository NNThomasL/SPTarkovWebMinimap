using System.Collections.Generic;
using EFT.Interactive;
using TechHappy.MapLocation.Common.Requests.Data;

namespace TechHappy.MapLocation.Services.Quests
{
    public interface IQuestDataService
    {
        IReadOnlyList<QuestData> QuestMarkers { get; }

        void ReloadQuestData(TriggerWithId[] allTriggers);
    }
}