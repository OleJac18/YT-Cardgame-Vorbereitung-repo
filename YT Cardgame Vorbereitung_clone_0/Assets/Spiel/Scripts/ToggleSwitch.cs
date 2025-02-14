using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider setup")]
    [SerializeField, Range(0, 1f)] private float _sliderValue;

    public bool CurrentValue { get; private set; }

    private Slider _slider;

    [Header("Animation")]
    [SerializeField, Range(0, 1f)] private float _animationDuration = 0.5f;
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine _animateSliderCoroutine;


    [SerializeField] public UnityEvent _onToggleOn;
    [SerializeField] public UnityEvent _onToggleOff;

    private ToggleSwitchGroupManager _toggleSwitchGroupManager;

    protected void OnValidate()
    {
        SetupToggleComponents();

        _slider.value = _sliderValue;
    }

    private void SetupToggleComponents()
    {
        if (_slider != null)
            return;

        SetupSliderComponent();
    }

    private void SetupSliderComponent()
    {
        _slider = GetComponent<Slider>();

        if(_slider == null)
        {
            Debug.Log("No slider found!", this);
            return;
        }

        _slider.interactable = false;
        var SliderColors = _slider.colors;
        SliderColors.disabledColor = Color.white;
        _slider.colors = SliderColors;
        _slider.transition = Selectable.Transition.None;
    }

    public void SetupForManager(ToggleSwitchGroupManager manager)
    {
        _toggleSwitchGroupManager = manager;
    }

    private void Awake()
    {
        SetupToggleComponents();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    private void Toggle()
    {
        if (_toggleSwitchGroupManager != null)
            _toggleSwitchGroupManager.ToggleGroup(this);
        else
            SetStateAndStartAnimation(!CurrentValue);
    }

    public void ToggleByGroupManager(bool valueToSetTo)
    {
        SetStateAndStartAnimation(valueToSetTo);
    }

    private void SetStateAndStartAnimation(bool state)
    {
        CurrentValue = state;

        if (CurrentValue)
            _onToggleOn?.Invoke();
        else
            _onToggleOff?.Invoke();

        if (_animateSliderCoroutine != null)
            StopCoroutine(_animateSliderCoroutine);

        _animateSliderCoroutine = StartCoroutine(AnimateSlider());
    }

    private IEnumerator AnimateSlider()
    {
        float startValue = _slider.value;
        float endValue = CurrentValue ? 1 : 0;

        float time = 0;
        if (_animationDuration > 0)
        {
            while (time < _animationDuration)
            {
                time += Time.deltaTime;

                float lerpFactor = slideEase.Evaluate(time / _animationDuration);
                _slider.value = _sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                yield return null;
            }
        }

        _slider.value = endValue;
    }
}
