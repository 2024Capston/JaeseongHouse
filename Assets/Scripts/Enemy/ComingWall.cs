using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ComingWall : NetworkBehaviour
{
    // 이동 속력, 회전 속력, 점프력
    [SerializeField] private float _walkSpeed = 1f;
    [SerializeField] private float _rotateSpeed = 2f;

    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수
    /// </summary>
    [SerializeField] private ColorType _initColor;
    [SerializeField]  private Transform _targetTransform;
    public bool _isFragile = false;



    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;
    private Renderer _objectRenderer;


    // 플레이어 조작에 쓰이는 보조 변수
    private Vector3 _pastPosition;
    private float _pitchAngle;

    /// <summary>
    /// 상자의 현재 색깔
    /// </summary>
    [SerializeField] private NetworkVariable<ColorType> _wallColor = new NetworkVariable<ColorType>();
    public NetworkVariable<ColorType> WallColor
    {
        get => _wallColor;
        // set => _wallColor.Value = value.Value;
    }
    private void Awake()
    {
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer && _initColor == ColorType.None)
        {
            _initColor = (ColorType)UnityEngine.Random.Range(1,3);
            _wallColor.Value = _initColor;
        }
        _meshRenderer = GetComponent<MeshRenderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();

        // 큐브의 색깔이 변하면 함수 호출하도록 지정
        _wallColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnWallColorChanged(before, after);
        };
        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _wallColor.OnValueChanged.Invoke(_wallColor.Value, _wallColor.Value);
            });
        }
        else
        {
            _wallColor.OnValueChanged.Invoke(_wallColor.Value, _wallColor.Value);
        }
    }
    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _objectRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        // 위치는 Owner(서버)에 의해서만 갱신되도록 한다
         if (!IsOwner)
        {
            return;
        }
        // transform.position += new Vector3(0, 0, -.3f);
        transform.position += new Vector3(0, 0, -_walkSpeed) * Time.deltaTime;
        //// 이동
        //Vector3 moveDir = (_targetTransform.position - transform.position).normalized * _walkSpeed;
        //_rigidbody.velocity = new Vector3(moveDir.x, _rigidbody.velocity.y, moveDir.z);
        if (transform.position.z < -5)
        {
            GameManager.instance.destroyWall(SideType.Left, gameObject);
            GameManager.instance.destroyWall(SideType.Right, gameObject);
        }
    }
    /// <summary>
    /// 상자의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnWallColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0) : new Color(0, 0, 1);
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        //int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        // 색깔이 다른 물체는 투명도 추가
        if (after != MultiplayerManager.Instance.LocalPlayer.PlayerColor.Value)                                                             
        {
            newColor.a = 0.7f;
        }

        _meshRenderer.material.color = newColor;
        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        //_boxCollider.excludeLayers = excludedLayer;

    }
}
