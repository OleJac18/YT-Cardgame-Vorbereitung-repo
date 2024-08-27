using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI numberTextTopLeft;
    [SerializeField] private TextMeshProUGUI numberTextBottomRight;
    [SerializeField] private GameObject cardBackImage;

    private Vector3 _originalScale;
    private Vector3 _hoverScale;

    private void Awake()
    {
        _originalScale = Vector3.one;
        _hoverScale = new Vector3(1.1f, 1.1f, 1f);
    }

    private void Start()
    {
        numberTextTopLeft.text = "1";
        numberTextBottomRight.text = "1";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Ich habe die Maus auf die Karte bewegt.");

        // Verändert die Kartengröße
        this.transform.localScale = _hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Ich habe die Maus von der Karte herunter bewegt.");

        // Verändert die Kartengröße
        this.transform.localScale = _originalScale;
    }
}
