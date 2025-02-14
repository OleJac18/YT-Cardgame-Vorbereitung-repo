using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldDefault : MonoBehaviour
{
    // Referenz auf das InputField
    public TMP_InputField inputField;

    // Default-Wert
    public string defaultValue = "127.0.0.1";

    void Start()
    {
        // Setzt den Default-Wert in das InputField, wenn es leer ist
        if (inputField != null && string.IsNullOrEmpty(inputField.text))
        {
            inputField.text = defaultValue;
        }
    }
}
