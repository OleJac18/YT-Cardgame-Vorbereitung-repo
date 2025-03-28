using System.Collections.Generic;
using Unity.Netcode;

[System.Serializable]
public class Player : INetworkSerializable
{
    public ulong id;
    public List<int> cards = new List<int>();
    public string name;
    public int totalScore;
    public int roundScore;

    public Player()
    {

    }

    public Player(ulong id, List<int> cards, string name, int totalScore, int roundScore)
    {
        this.id = id;
        this.cards = cards;
        this.name = name;
        this.totalScore = totalScore;
        this.roundScore = roundScore;
    }



    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref totalScore);
        serializer.SerializeValue(ref roundScore);

        // Serialisierung für List<int>
        // Für die Deserialisierung wichtig, damit die Variable count initialisiert war,
        // bevor aus dem Stream mit serializer.SerializeValue(ref count) gelesen werden kann
        int count = cards.Count;

        // Serialisierung und Deserialisierung, weil dies bidirektional funktioniert
        serializer.SerializeValue(ref count);

        // Nur beim Deserialisieren
        //      Warum?
        // - Beim Lesen wird das Player-Objekt zwar erstellt, aber die Liste cards existiert noch nicht oder ist leer.
        // - Die Liste muss neu initialisiert werden, um Platz für die zu deserialisierenden Elemente zu schaffen.
        if (serializer.IsReader)
        {
            cards = new List<int>(count);
        }



        for (int i = 0; i < count; i++)
        {
            // Für die Deserialisierung wichtig, damit die Variable card initialisiert war,
            // bevor aus dem Stream mit serializer.SerializeValue(ref card) gelesen werden kann.
            // Es ist zwar die Größe der Liste angegeben worden aber diese noch nicht mit Elementen
            // gefüllt worden. Deshalb sicher wir uns ab, in dem wir im Notfall eine 0 reinschreiben
            int card = cards.Count > i ? cards[i] : 0;

            serializer.SerializeValue(ref card);

            // Nur beim Deserialisieren. Es werden die einzelnen Elemente manuell in der Liste abgespeichert
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
