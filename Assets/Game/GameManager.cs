using RTS.Network;
using RTS.Sim;
using UnityEngine;

namespace RTS.Game
{
    public enum GameState { WaitingToConnect, Connecting, WaitingForPlayers, Playing, Ended }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.WaitingToConnect;
        public RtsClient Client { get; private set; }
        public LockstepRunner Runner { get; private set; }
        public byte LocalPlayerID => Client?.PlayerID ?? 0;
        public byte CurrentN => Client?.CurrentN ?? 3;

        [SerializeField] private int _unitsPerPlayer = 5;

        public event System.Action OnGameStarted;
        public event System.Action<string> OnError;

        private float _retxTimer;
        private const float RetxInterval = 0.02f;

        private void Awake()
        {
            Instance = this;
            Runner = GetComponent<LockstepRunner>();
            if (Runner == null) Runner = gameObject.AddComponent<LockstepRunner>();
        }

        public void Connect(string host, int port, string playerName, string roomID)
        {
            Client = new RtsClient();
            Client.OnError += err => OnError?.Invoke(err);
            Client.Connect(host, port, playerName, roomID);
            State = GameState.Connecting;
        }

        private void Update()
        {
            if (Client == null) return;

            Client.Update();

            if (State == GameState.Connecting && Client.State == RtsClient.ClientState.InRoom)
            {
                StartGame();
            }

            _retxTimer += Time.deltaTime;
            if (_retxTimer >= RetxInterval)
            {
                _retxTimer -= RetxInterval;
                Client.Tick();
            }
        }

        private void StartGame()
        {
            State = GameState.Playing;

            Runner.Init(Client, Client.Seed, Client.MapW, Client.MapH);

            LoadSinTable();

            for (int i = 0; i < _unitsPerPlayer; i++)
            {
                Runner.World.SpawnUnit(0,
                    new Vec2(Fixed32.FromInt(10 + i * 5), Fixed32.FromInt(50)),
                    Fixed32.One, new Fixed32(32768));
                Runner.World.SpawnUnit(1,
                    new Vec2(Fixed32.FromInt(90 - i * 5), Fixed32.FromInt(50)),
                    Fixed32.One, new Fixed32(32768));
            }

            OnGameStarted?.Invoke();
        }

        private void LoadSinTable()
        {
            var asset = Resources.Load<TextAsset>("GoldenData");
            if (asset != null)
            {
                var golden = JsonUtility.FromJson<GoldenDataResource>(asset.text);
                Trig.InitSinTable(golden.sinTableRaw);
            }
            else
            {
                Debug.LogWarning("GoldenData.json not found in Resources — sin table not initialized!");
            }
        }

        private void OnDestroy()
        {
            Client?.Dispose();
        }

        [System.Serializable]
        private class GoldenDataResource
        {
            public int[] sinTableRaw;
        }
    }
}
