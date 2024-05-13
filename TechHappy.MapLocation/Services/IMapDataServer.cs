using System;
using EFT.Interactive;

namespace TechHappy.MapLocation.Services
{
    public interface IMapDataServer : IDisposable
    {
        DateTime? LastQuestChangeTime { get; set; }

        void StartServer(string ipAddress, int port);

        void OnGameStarted();

        void OnGameEnded();

        void OnQuestsChanged(TriggerWithId[] allTriggers);
    }
}