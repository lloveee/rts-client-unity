namespace RTS.Network
{
    public enum MsgType : byte
    {
        Hello = 1,
        HelloAck = 2,
        JoinRoom = 3,
        JoinAck = 4,
        Cmd = 10,
        FrameBundle = 11,
        HashAck = 12,
        RTTReport = 13,
        NPub = 14,
        FeedbackHint = 15,
        Resume = 20,
        Resync = 21,
        Bye = 30
    }

    public class Hello
    {
        public ushort ProtocolVersion;
        public string PlayerName;
    }

    public class HelloAck
    {
        public ushort ProtocolVersion;
        public ushort ServerTickRate;
        public bool Accepted;
    }

    public class JoinRoom
    {
        public string RoomID;
    }

    public class JoinAck
    {
        public string RoomID;
        public byte PlayerID;
        public ulong Seed;
        public int MapW;
        public int MapH;
        public bool Accepted;
    }

    public class WireCmd
    {
        public uint Tick;
        public byte Player;
        public byte Op;
        public uint UnitID;
        public int TargetX;
        public int TargetY;
        public uint TargetID;
    }

    public class FrameBundle
    {
        public uint Tick;
        public byte NCurrent;
        public WireCmd[] Cmds;
    }

    public class HashAck
    {
        public uint Tick;
        public ulong Hash;
    }

    public class RTTReport
    {
        public ushort[] Samples = new ushort[3];
    }

    public class NPub
    {
        public uint EffectiveFromTick;
        public byte N;
    }

    public class FeedbackHint
    {
        public uint Tick;
        public byte Player;
        public ushort HintID;
    }

    public class WireResume
    {
        public ushort ConnID;
        public uint LastExecutedTick;
        public byte[] Token = new byte[16];
    }
}
