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
using UnityEngine.UIElements;

enum MoveDirection { Up, Down, Left, Right, Forward, Back } // 벽 이동 방향
enum MovementType { None, Pushable, Linear, Oscillating }   //벽움직임의 종류
public class WallController : NetworkBehaviour
{
    // 각종 설정 체크 변수
    [SerializeField] private MovementType _movementType;
    [SerializeField] private MoveDirection _moveDirection;
    [SerializeField] private float _movingSpeed = 1f;
    [SerializeField] private float _moveDistance = 0f;

    [SerializeField] private bool _isBouncy = false;    // 닿으면 튕기는지?
    [SerializeField] private bool _isDeadly = false;    // 닿으면 죽는지?


    [SerializeField] private float _rotateSpeed = 2f;

    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수
    /// </summary>
    [SerializeField] private ColorType _initColor;
    [SerializeField]  private Transform _targetTransform;
    public bool _isFragile = false;

    private Vector3 _spawnedPosition;


    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;
    private Renderer _objectRenderer;


    // 플레이어 조작에 쓰이는 보조 변수
    private Vector3 _pastPosition;
    private float _pitchAngle;
    private Vector3 _movingVector;

    private float _turnedTime = 0;

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
            _initColor = (ColorType)UnityEngine.Random.Range(1, 4);
        }
        _wallColor.Value = _initColor;
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
        _pastPosition = transform.position;
    }
    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _objectRenderer = GetComponent<Renderer>();
        _spawnedPosition = transform.position;
        _movingVector = Vector3.zero;
        // 방향에 따라 이동 벡터 설정
        switch (_moveDirection)
        {
            case MoveDirection.Up:
                _movingVector = Vector3.up;
                break;
            case MoveDirection.Down:
                _movingVector = Vector3.down;
                break;
            case MoveDirection.Left:
                _movingVector = Vector3.left;
                break;
            case MoveDirection.Right:
                _movingVector = Vector3.right;
                break;
            case MoveDirection.Forward:
                _movingVector = Vector3.forward;
                break;
            case MoveDirection.Back:
                _movingVector = Vector3.back;
                break;
        }
        _turnedTime = 0;
    }

    private void Update()
    {
        // 위치는 (서버)에 의해서만 갱신되도록 한다
        if (!IsServer)
        {
            return;
        }

        switch (_movementType)              
        {
            case MovementType.None:
                break;
            case MovementType.Pushable:
                break;
            case MovementType.Linear:
                transform.position += _movingVector * _movingSpeed * Time.deltaTime;
                if (Vector3.Distance(transform.position, _spawnedPosition) >= _moveDistance)
                {
                    Destroy(gameObject);
                }
                break;
            case MovementType.Oscillating:
                if (Vector3.Distance(transform.position, _spawnedPosition) >= _moveDistance && _turnedTime<=0)
                {
                    _movingVector = -_movingVector;
                    transform.position = _pastPosition;
                    _turnedTime += 0.1f;
                }
                transform.position += _movingVector * _movingSpeed * Time.deltaTime;
                break;
        }
        if (_turnedTime > 0) _turnedTime -= Time.deltaTime;


        _pastPosition = transform.position;

        // transform.position += new Vector3(0, 0, -.3f);
        //// 이동
        //Vector3 moveDir = (_targetTransform.position - transform.position).normalized * _movingSpeed;
        //_rigidbody.velocity = new Vector3(moveDir.x, _rigidbody.velocity.y, moveDir.z);
    }
    /// <summary>
    /// 상자의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnWallColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0) : (after == ColorType.Blue) ? new Color(0, 0, 1) : new Color(1, 0, 1);

        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : (after == ColorType.Blue) ? LayerMask.NameToLayer("Blue") : LayerMask.NameToLayer("Purple");
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
    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        Rigidbody collisionRB = collision.gameObject.GetComponent<Rigidbody>();
        switch (_movementType)
        {
            case MovementType.None:
                break;
            case MovementType.Pushable:
                break;
            case MovementType.Linear:
                break;
            case MovementType.Oscillating:
                break;
        }
        if (_isBouncy)  // 벽에서 플레이
        {
            collisionRB.AddForce((collision.transform.position - transform.position).normalized * 5);
        }
        if (_isDeadly)
        {
            if (collision.gameObject.tag == "Player")
            {
                collision.gameObject.GetComponent<PlayerController>().repositionPlayer();
                                                
            }
        }
    }
}
