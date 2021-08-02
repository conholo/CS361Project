using System.Collections;
using UnityEngine;

public class WeatherPanelHover : MonoBehaviour
{
    [SerializeField] private bool _shouldAnimate = true;
    [SerializeField] private float _animationTime = 0.5f;
    [SerializeField] private RectTransform _rectToAnimate;
    
    private RectTransform _rect;

    private bool _animating;
    
    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (!_shouldAnimate) return;
        
        var mousePosition = _rect.InverseTransformPoint(Input.mousePosition);

        if (_rect.rect.Contains(mousePosition) && !_animating)
            StartCoroutine(Animate(false));
        else if (!_rect.rect.Contains(mousePosition) && !_animating)
            StartCoroutine(Animate(true));

    }

    private IEnumerator Animate(bool animateIn)
    {
        _animating = true;
        
        var startingScale = _rectToAnimate.transform.localScale;
        var targetScaleX = animateIn ? 0.0f : 1.0f;

        var timer = 0.0f;
        
        while (timer < _animationTime)
        {
            timer += Time.deltaTime;

            var newScaleX = Mathf.Lerp(startingScale.x, targetScaleX, timer / _animationTime);
            _rectToAnimate.transform.localScale = new Vector3(newScaleX, startingScale.y, startingScale.z);
            
            yield return null;
        }
        
        _rectToAnimate.transform.localScale = new Vector3(targetScaleX, startingScale.y, startingScale.z);
        _animating = false;
    }
}