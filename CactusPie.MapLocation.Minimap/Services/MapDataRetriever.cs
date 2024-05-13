using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Services.Interfaces;
using RestSharp;
using TechHappy.MapLocation.Common.Requests;
using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Services;

public sealed class MapDataRetriever : IMapDataRetriever
{
    private readonly ICurrentMapData _currentMapData;

    private readonly RestClient _restClient;


    private CancellationTokenSource? _dataReceivingCancellationToken;

    public MapDataRetriever(ICurrentMapData currentMapData, RestClient restClient)
    {
        _currentMapData = currentMapData;
        _restClient = restClient;
    }

    public void StartReceivingData()
    {
        if (_dataReceivingCancellationToken != null)
        {
            throw new InvalidOperationException("The receive operation is already running");
        }

        _dataReceivingCancellationToken = new CancellationTokenSource();

        Task.Run(
            async () =>
            {
                CancellationToken cancellationToken = _dataReceivingCancellationToken.Token;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = new RestRequest("mapData");

                    RestResponse<MapLocationResponse> response = await _restClient
                        .ExecuteAsync<MapLocationResponse>(request, cancellationToken)
                        .ConfigureAwait(false);

                    if (!response.IsSuccessful || response.Data is not { IsGameInProgress: true })
                    {
                        _currentMapData.IsGameInProgress = false;
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    MapLocationResponse? mapData = response.Data;

                    List<BotLocation>? botLocations = mapData.BotLocations?.Select(
                            botLocation =>
                                new BotLocation(
                                    botLocation.BotId,
                                    botLocation.BotType,
                                    botLocation.XPosition,
                                    botLocation.YPosition,
                                    botLocation.ZPosition))
                        .ToList() ?? new List<BotLocation>(0);

                    _currentMapData.LastReceivedPosition = new MapPositionData(
                        mapData.MapName,
                        mapData.XPosition,
                        mapData.YPosition,
                        mapData.ZPosition,
                        mapData.XRotation,
                        mapData.YRotation,
                        mapData.Airdrop,
                        mapData.LastQuestChangeTime,
                        botLocations);

                    _currentMapData.IsGameInProgress = response.Data.IsGameInProgress;

                    await Task.Delay(_currentMapData.PositionRefreshRate, cancellationToken);
                }
            });
    }

    public async Task<IReadOnlyList<QuestData>?> GetQuestData(CancellationToken cancellationToken)
    {
        var request = new RestRequest("quests");

        RestResponse<QuestDataResponse> response = await _restClient.ExecuteAsync<QuestDataResponse>(
                request,
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessful || response.Data == null)
        {
            _currentMapData.IsGameInProgress = false;
            return null;
        }

        QuestDataResponse questData = response.Data;

        if (questData == null)
        {
            return null;
        }

        return questData.Quests;
    }

    public void StopReceivingData()
    {
        _dataReceivingCancellationToken?.Cancel();
    }

    public void Dispose()
    {
        if (_dataReceivingCancellationToken != null)
        {
            _dataReceivingCancellationToken?.Cancel();
            _dataReceivingCancellationToken?.Dispose();
        }
    }
}