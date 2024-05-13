using System;
using System.Collections.Generic;
using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Events;

public class QuestDataReceivedEventArgs : EventArgs
{
    public QuestDataReceivedEventArgs(IReadOnlyList<QuestData>? quests)
    {
        Quests = quests;
    }

    public IReadOnlyList<QuestData>? Quests { get; set; }
}