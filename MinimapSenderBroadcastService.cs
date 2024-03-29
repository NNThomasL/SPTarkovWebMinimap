using Comfort.Common;
using EFT;
using NetCoreServer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using TechHappy.MinimapSender;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TechHappy.MinimapSender
{
    // This class handles the websocket server and it's events. Has a timer that sends the websocket message every interval (configured in F12 menu).
    public sealed class MinimapSenderBroadcastService : IDisposable
    {
        private readonly GamePlayerOwner
            _gamePlayerOwner; // GamePlayerOwner object that is sent from the mod's Controller object.        

        private readonly Timer
            _timer; // The timer handles the sending of the map data updates over the websocket.        

        private List<QuestMarkerInfo> _quests; // Array of quest data objects to be sent each message

        public MinimapSenderBroadcastService(GamePlayerOwner gamePlayerOwner)
        {
            _gamePlayerOwner = gamePlayerOwner;
            _timer = new Timer
            {
                AutoReset = true,
                Interval = 250, // Initial value that isn't used. Configured in StartBroadcastingPosition().
            };
            _timer.Elapsed += HandleTimer;
        }

        private void HandleTimer(object sender, ElapsedEventArgs args)
        {
            try
            {
                // Send a new map update message over the websocket.
                SendData();
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError(
                    $"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        public void StartBroadcastingPosition(double interval = 250)
        {
            _timer.Interval = interval;
            _timer.Start();

            try
            {
                SendData(); // Send a new map update message over the websocket.
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError(
                    $"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        public void StopBroadcastingPosition()
        {
            _timer.Stop();
        }

        public void ChangeInterval(double interval)
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _timer.Interval = interval;
                _timer.Start();
            }
            else
            {
                _timer.Interval = interval;
            }
        }

        private void SendData()
        {
            try
            {
                if (_gamePlayerOwner.Player != null)
                {
                    var msg = ConstructMessage();
                    MinimapSenderPlugin._server.MulticastText(msg);
                }
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError(
                    $"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        private string ConstructMessage()
        {
            string msgType = "mapData";
            string mapName = _gamePlayerOwner.Player.Location;
            Vector3[] airdrops = MinimapSenderPlugin.ShowAirdrops.Value
                ? MinimapSenderPlugin.airdrops.ToArray()
                : new Vector3[] { };
            Vector3 playerPosition = _gamePlayerOwner.Player.Position;
            Vector2 playerRotation = _gamePlayerOwner.Player.Rotation;
            var quests = MinimapSenderPlugin.ShowQuestMarkers.Value ? _quests : new List<QuestMarkerInfo>();

            return JsonConvert.SerializeObject(new
            {
                msgType = msgType,
                raidCounter = MinimapSenderPlugin.raidCounter,
                mapName = mapName,
                playerRotationX = playerRotation.x,
                playerPositionX = playerPosition.x,
                playerPositionZ = playerPosition.z,
                playerPositionY = playerPosition.y,
                airdrops = airdrops,
                quests = quests
            });
        }

        public void UpdateQuestData(List<QuestMarkerInfo> questData)
        {
            _quests = questData;
        }


        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}


class WebsocketSession : WsSession
{
    public WebsocketSession(WsServer server) : base(server)
    {
    }

    public override void OnWsConnected(HttpRequest request)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session with Id {Id} connected!");
    }

    public override void OnWsDisconnected()
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session with Id {Id} disconnected!");
    }

    /// <summary>
    /// Called when a WebSocket session receives data.
    /// When a "get_connect_address" event is received, it sends back a "connectAddress" message with the game's local IP
    /// </summary>
    /// <param name="buffer">The received data buffer.</param>
    /// <param name="offset">The offset in the buffer where the received data starts.</param>
    /// <param name="size">The size of the received data in bytes.</param>
    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

        if (values["type"] != null && values["type"] == "get_connect_address")
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string msgType = "connectAddress";

                    MinimapSenderPlugin._server.MulticastText(JsonConvert.SerializeObject(new
                    {
                        msgType = msgType,
                        ipAddress = ip.ToString(),
                    }));

                    break;
                }
            }
        }
    }

    protected override void OnError(SocketError error)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session caught an error with code {error}");
    }
}

class MinimapServer : WsServer
{
    public MinimapServer(IPAddress address, int port) : base(address, port)
    {
    }

    protected override TcpSession CreateSession()
    {
        return new WebsocketSession(this);
    }

    protected override void OnError(SocketError error)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogError($"Minimap WebSocket server caught an error with code {error}");
    }
}