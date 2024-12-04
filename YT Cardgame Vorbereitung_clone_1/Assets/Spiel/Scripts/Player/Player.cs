using System.Collections.Generic;
using Unity.Netcode;

[System.Serializable]
public class Player : INetworkSerializable
{
    public ulong id;
    public List<int> cards = new List<int>();
    public string name;
    public int score;

    public Player()
    {

    }

    public Player(ulong id, List<int> cards, string name, int score)
    {
        this.id = id;
        this.cards = cards;
        this.name = name;
        this.score = score;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref score);

        // Serialisierung für List<int>
        int count = cards.Count;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            cards = new List<int>(count);
        }

        for (int i = 0; i < count; i++)
        {
            int card = cards.Count > i ? cards[i] : 0;
            serializer.SerializeValue(ref card);
            if (serializer.IsReader)
            {
                if (i < cards.Count)
                    cards[i] = card;
                else
                    cards.Add(card);
            }
        }
    }
}
