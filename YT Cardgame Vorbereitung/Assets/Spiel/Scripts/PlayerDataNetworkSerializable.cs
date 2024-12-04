using Unity.Netcode;

public class PlayerDataNetworkSerializable : INetworkSerializable
{
    public ulong id;
    public string name;
    public int score;
    // INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref score);
    }
}
