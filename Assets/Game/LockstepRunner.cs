using RTS.Sim;
using RTS.Network;
using UnityEngine;

namespace RTS.Game
{
    public class LockstepRunner : MonoBehaviour
    {
        public World World { get; private set; }
        public uint CurrentTick => World?.Tick ?? 0;

        private RtsClient _client;
        private float _timeSinceLastTick;
        public float TickInterval => 0.05f;
        public float TimeSinceLastTick => _timeSinceLastTick;

        public event System.Action OnTickAdvanced;

        public void Init(RtsClient client, ulong seed, int mapW, int mapH)
        {
            _client = client;
            World = new World(seed, mapW, mapH);
            _client.OnFrame += HandleFrame;
        }

        private void Update()
        {
            _timeSinceLastTick += Time.deltaTime;
        }

        private void HandleFrame(FrameBundle fb)
        {
            var cmds = new Cmd[fb.Cmds.Length];
            for (int i = 0; i < fb.Cmds.Length; i++)
            {
                var wc = fb.Cmds[i];
                cmds[i] = new Cmd
                {
                    Player = wc.Player,
                    Op = (CmdOp)wc.Op,
                    UnitID = wc.UnitID,
                    TargetPos = new Vec2(
                        Fixed32.FromRaw(wc.TargetX),
                        Fixed32.FromRaw(wc.TargetY)),
                    TargetID = wc.TargetID
                };
            }

            SimStep.Step(World, cmds);
            ulong hash = SimHash.Hash(World);
            _client.SendHashAck(World.Tick, hash);

            _timeSinceLastTick = 0f;
            OnTickAdvanced?.Invoke();
        }

        private void OnDestroy()
        {
            if (_client != null)
                _client.OnFrame -= HandleFrame;
        }
    }
}
