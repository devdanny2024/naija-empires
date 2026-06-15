using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace NaijaEmpires
{
    /// M2 Phase 1 — CONNECTIVITY SCAFFOLD (temporary IMGUI lobby).
    /// Gets two peers into one host-authoritative Fusion session by room code, and reports the
    /// player count. This proves the Photon connection end-to-end BEFORE the full host-authoritative
    /// sim refactor + the real branded lobby UI. App Id is read from PhotonAppSettings.
    public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        NetworkRunner _runner;
        string _code = "naija";
        string _status = "Not connected";
        bool _busy;

        async Task Launch(GameMode mode)
        {
            if (_runner != null || _busy) return;
            _busy = true;
            _status = $"{mode}: connecting to '{_code}'…";

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = _code,
                PlayerCount = 2,
            });

            if (result.Ok)
                _status = $"{mode} OK — room '{_code}'. Players: {_runner.SessionInfo.PlayerCount}";
            else
            {
                _status = $"{mode} FAILED — {result.ShutdownReason} (App Id set?)";
                Destroy(_runner);
                _runner = null;
            }
            _busy = false;
        }

        void OnGUI()
        {
            const float w = 340f, h = 132f;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, 8f, w, h), GUI.skin.box);
            GUILayout.Label("◈ MULTIPLAYER (test scaffold)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Room code", GUILayout.Width(78));
            _code = GUILayout.TextField(_code ?? "");
            GUILayout.EndHorizontal();

            GUI.enabled = _runner == null && !_busy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host")) _ = Launch(GameMode.Host);
            if (GUILayout.Button("Join")) _ = Launch(GameMode.Client);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _status = $"Player {player.PlayerId} joined — {runner.SessionInfo.PlayerCount} in room ({(runner.IsServer ? "host" : "client")})";
            Debug.Log($"[NE-Net] Player joined {player}. Count={runner.SessionInfo.PlayerCount}, IsServer={runner.IsServer}");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
            => _status = $"Player {player.PlayerId} left — {runner.SessionInfo.PlayerCount} in room";

        public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("[NE-Net] Connected to server.");
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => _status = $"Disconnected: {reason}";
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => _status = $"Connect failed: {reason}";
        public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { _status = $"Shutdown: {reason}"; _runner = null; _busy = false; }

        // --- remaining INetworkRunnerCallbacks: unused in Phase 1 (exact Fusion 2 signatures) ---
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
