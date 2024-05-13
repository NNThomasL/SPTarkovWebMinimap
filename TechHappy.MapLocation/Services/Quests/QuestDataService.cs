using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using TechHappy.MapLocation.Common.Requests.Data;

namespace TechHappy.MapLocation.Services.Quests
{
    public sealed class QuestDataService : IQuestDataService
    {
        private readonly ILocalizationHelper _localizationHelper;

        public QuestDataService(ILocalizationHelper localizationHelper)
        {
            _localizationHelper = localizationHelper;
        }

        public IReadOnlyList<QuestData> QuestMarkers { get; private set; } = Array.Empty<QuestData>();

        public void ReloadQuestData(TriggerWithId[] allTriggers)
        {
            var questMarkerData = new List<QuestData>(32);

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            Player player = gameWorld.MainPlayer;

            var questData = Traverse.Create(player).Field("_questController").GetValue<object>();

            var quests = Traverse.Create(questData).Property("Quests").GetValue<object>();

            var questsList = Traverse.Create(quests).Field("list_1").GetValue<IList>();

            var lootItemsList = Traverse.Create(gameWorld).Field("LootItems").Field("list_0").GetValue<List<LootItem>>();

            (string Id, LootItem Item)[] questItems =
                lootItemsList.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();

            foreach (QuestDataClass item in questsList)
            {
                if (item.Status != EQuestStatus.Started)
                {
                    continue;
                }

                var template = Traverse.Create(item).Field("Template").GetValue<RawQuestClass>();

                var nameKey = Traverse.Create(template).Property("NameLocaleKey").GetValue<string>();

                var traderId = Traverse.Create(template).Property("TraderId").GetValue<string>();
                
                var questConditions = Traverse.Create(template).Property("Conditions").GetValue<IDictionary>();

                foreach (DictionaryEntry conditionList in questConditions)
                {
                    if ((EQuestStatus)conditionList.Key != EQuestStatus.AvailableForFinish)
                    {
                        continue;
                    }
                    
                    var conditionsAvailable = Traverse.Create(conditionList.Value).Field("list_0").GetValue();

                    foreach (Condition condition in conditionsAvailable as List<Condition>)
                    {
                        // Check if this part of the quest has already been completed
                        if (item.CompletedConditions.Contains(condition.id))
                        {
                            continue;
                        }
                        
                        switch (condition)
                        {
                            case ConditionLeaveItemAtLocation location:
                            {
                                string zoneId = location.zoneId;
                                IEnumerable<PlaceItemTrigger> zoneTriggers = allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

                                if (zoneTriggers != null)
                                {
                                    foreach (PlaceItemTrigger trigger in zoneTriggers)
                                    {
                                        var staticInfo = new QuestData
                                        {
                                            Id = location.id,
                                            Location = ToQuestLocation(trigger.transform.position),
                                            ZoneId = zoneId,
                                            NameText = _localizationHelper.Localized(nameKey),
                                            Description = _localizationHelper.Localized(location.id),
                                            Trader = TraderIdToName(traderId),
                                        };

                                        questMarkerData.Add(staticInfo);
                                    }
                                }

                                break;
                            }
                            case ConditionPlaceBeacon beacon:
                            {
                                string zoneId = beacon.zoneId;

                                IEnumerable<PlaceItemTrigger> zoneTriggers = allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

                                if (zoneTriggers != null)
                                {
                                    foreach (PlaceItemTrigger trigger in zoneTriggers)
                                    {
                                        var staticInfo = new QuestData
                                        {
                                            Id = beacon.id,
                                            Location = ToQuestLocation(trigger.transform.position),
                                            ZoneId = zoneId,
                                            NameText = _localizationHelper.Localized(nameKey),
                                            Description = _localizationHelper.Localized(beacon.id),
                                            Trader = TraderIdToName(traderId),
                                        };

                                        questMarkerData.Add(staticInfo);
                                    }
                                }

                                break;
                            }
                            case ConditionFindItem findItem:
                            {
                                string[] itemIds = findItem.target;

                                foreach (string itemId in itemIds)
                                {
                                    foreach ((string Id, LootItem Item) questItem in questItems)
                                    {
                                        if (questItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var staticInfo = new QuestData
                                            {
                                                Id = findItem.id,
                                                Location = ToQuestLocation(questItem.Item.transform.position),
                                                NameText = _localizationHelper.Localized(nameKey),
                                                Description = _localizationHelper.Localized(findItem.id),
                                                Trader = TraderIdToName(traderId),
                                            };

                                            questMarkerData.Add(staticInfo);
                                        }
                                    }
                                }

                                break;
                            }
                            case ConditionCounterCreator counterCreator:
                            {
                                var conditionsList = Traverse.Create(counterCreator.Conditions).Field("list_0").GetValue<IList>();

                                foreach (object condition2 in conditionsList)
                                {
                                    switch (condition2)
                                    {
                                        case ConditionVisitPlace place:
                                        {
                                            string zoneId = place.target;

                                            IEnumerable<ExperienceTrigger> zoneTriggers =
                                                allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

                                            if (zoneTriggers != null)
                                            {
                                                foreach (ExperienceTrigger trigger in zoneTriggers)
                                                {
                                                    var staticInfo = new QuestData
                                                    {
                                                        Id = counterCreator.id,
                                                        Location = ToQuestLocation(trigger.transform.position),
                                                        ZoneId = zoneId,
                                                        NameText = _localizationHelper.Localized(nameKey),
                                                        Description = _localizationHelper.Localized(counterCreator.id),
                                                        Trader = TraderIdToName(traderId),
                                                    };

                                                    questMarkerData.Add(staticInfo);
                                                }
                                            }

                                            break;
                                        }
                                        case ConditionInZone inZone:
                                        {
                                            string[] zoneIds = inZone.zoneIds;

                                            foreach (string zoneId in zoneIds)
                                            {
                                                IEnumerable<ExperienceTrigger> zoneTriggers =
                                                    allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

                                                if (zoneTriggers != null)
                                                {
                                                    foreach (ExperienceTrigger trigger in zoneTriggers)
                                                    {
                                                        var staticInfo = new QuestData
                                                        {
                                                            Id = counterCreator.id,
                                                            Location = ToQuestLocation(trigger.transform.position),
                                                            ZoneId = zoneId,
                                                            NameText = _localizationHelper.Localized(nameKey),
                                                            Description = _localizationHelper.Localized(counterCreator.id),
                                                            Trader = TraderIdToName(traderId),
                                                        };

                                                        questMarkerData.Add(staticInfo);
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }

            QuestMarkers = questMarkerData;
        }

        private static QuestLocation ToQuestLocation(UnityEngine.Vector3 vector)
        {
            return new QuestLocation(vector.x, vector.y, vector.z);
        }

        private string TraderIdToName(string traderId)
        {
            if (traderId.Equals("5ac3b934156ae10c4430e83c", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Ragman";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb57776803fa99248b456e", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Therapist";
            }

            if (traderId.Equals("579dc571d53a0658a154fbec", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Fence";
            }

            if (traderId.Equals("58330581ace78e27b8b10cee", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Skier";
            }

            if (traderId.Equals("5935c25fb3acc3127c3d8cd9", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Peacekeeper";
            }

            if (traderId.Equals("5a7c2eca46aef81a7ca2145d", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Mechanic";
            }

            if (traderId.Equals("5c0647fdd443bc2504c2d371", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Jaeger";
            }

            if (traderId.Equals("638f541a29ffd1183d187f57", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Lighthouse Keeper ";
            }

            return _localizationHelper.Localized(traderId);
        }
    }
}