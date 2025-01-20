using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RenderLine : MonoBehaviour {
    // Use this for initialization
    [SerializeField] private RaycastHit _hit;
    [SerializeField] private float _maxDistance=20f;
    [SerializeField] private Color _startColor;
    [SerializeField] private Color _endColor;
    [SerializeField] private float _startWidth;
    [SerializeField] private float _endWidth;
    [SerializeField] private float _renderDuration;
    [SerializeField] private float _renderTimer=-1f;
    [SerializeField] private Vector3 _targetPosition;


    bool clicked = false;
    LineRenderer _lineRenderer; 
    void Start () {

        _renderTimer=-1;
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.SetColors(_startColor, _endColor);
        _lineRenderer.SetWidth(_startWidth, _endWidth);
    }
    void Update()
    {
        if(_renderTimer>0)
        {
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, _targetPosition);
        }
        else if (_renderTimer<=0 && _renderTimer>-1)
        {
            _renderTimer-=Time.deltaTime;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position);
        }
    }
    public void DrawLine()
    {
        if(Physics.Raycast(transform.position, transform.forward, out _hit, _maxDistance))
        {
            _targetPosition= _hit.transform.position;
            _renderTimer=_renderDuration;
        }
    }
}