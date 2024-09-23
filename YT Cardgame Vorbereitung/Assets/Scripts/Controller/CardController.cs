using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI numberTextTopLeft;
    [SerializeField] private TextMeshProUGUI numberTextBottomRight;
    [SerializeField] private GameObject cardBackImage;

    private Outline _outline;
    private Vector3 _originalScale;
    private Vector3 _hoverScale;

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
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

        // Ver�ndert die Kartengr��e
        this.transform.localScale = _hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Ich habe die Maus von der Karte herunter bewegt.");

        // Ver�ndert die Kartengr��e
        this.transform.localScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Ich habe die Karte angeklickt.");
        SelectionAnimation();
    }

    /// <summary>
    /// F�hrt eine Auswahlanimation f�r die Karte aus. Wenn die Karte bereits ausgew�hlt ist, wird die Auswahl aufgehoben.
    /// Wenn die Karte nicht ausgew�hlt ist, wird sie ausgew�hlt.
    /// </summary>
    private void SelectionAnimation()
    {
        if (_outline == null)
        {
            Debug.Log("Das Object " + name + " hat keine Komponente Outline");
        }
        else
        {
            if (_outline.enabled)
            {
                _outline.enabled = false;
            }
            else
            {
                _outline.enabled = true;
            }
        }
    }

    // Dreht die Karte 180�. Nach 90� wird die Sichtbarkeit der R�ckseite invertiert
  /*  private void FlipCardAnimation(bool visible)
    {
        LeanTween.rotateY(this.gameObject, 90.0f, TimeForFlip).setOnComplete(() =>
        {
            this.SetCardBackImageVisibility(visible);
            LeanTween.rotateY(this.gameObject, 0.0f, TimeForFlip);
        });
    }*/
}
