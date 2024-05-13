using System.Collections.Generic;
using TechHappy.MapLocation.Common.Requests.Data;

namespace TechHappy.MapLocation.Common.Requests
{
    public sealed class QuestDataResponse
    {
        public IReadOnlyList<QuestData> Quests { get; set; }
    }
}