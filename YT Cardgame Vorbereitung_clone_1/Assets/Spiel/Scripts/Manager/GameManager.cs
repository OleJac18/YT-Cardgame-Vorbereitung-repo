using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event Action FlipAllCardsAtGameEndEvent;

    public void TriggerFlipAllCardsAtGameEnd()
    {
        FlipAllCardsAtGameEndEvent?.Invoke();
    }
}
