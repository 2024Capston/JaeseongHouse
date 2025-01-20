using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private ColorType _initColor;
    
    public NetworkVariable<ColorType> _bulletColor = new NetworkVariable<ColorType>();
    public NetworkVariable<ColorType> BulletColor
    {
        get => _bulletColor;
        set => _bulletColor.Value = value.Value;
    }
    private Rigidbody _rigidbody;
    private SphereCollider _sphereCollider;
    private MeshRenderer _meshRenderer;
    // Start is called before the first frame update
    void Awake()
    {
        _rigidbody = gameObject.GetComponent<Rigidbody>();
        _rigidbody.AddForce(new Vector3(0,0,5000));
    }

    public override void OnNetworkSpawn()
    {
        _bulletColor.Value = _initColor;

        _meshRenderer = GetComponent<MeshRenderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _sphereCollider = GetComponent<SphereCollider>();

        // 큐브의 색깔이 변하면 함수 호출하도록 지정
        _bulletColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnCubeColorChanged(before, after);
        };

        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _bulletColor.OnValueChanged.Invoke(_bulletColor.Value, _bulletColor.Value);
            });
        }
        else
        {
            _bulletColor.OnValueChanged.Invoke(_bulletColor.Value, _bulletColor.Value);
        }
    }
    public void setColor(ColorType color)
    {
        _bulletColor.Value=color;
    }
    // Update is called once per frame
    void Update()
    {
        // // 상자의 위치는 Owner에 의해서만 갱신되도록 한다 (서버 또는 들고 있는 사람)
        // if (!IsOwner)
        // {
        //     return;
        // }
    }

    /// <summary>
    /// 상자의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnCubeColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0) : new Color(0, 0, 1);
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        // 색깔이 다른 물체는 투명도 추가
        if (after != MultiplayerManager.Instance.LocalPlayer.PlayerColor.Value)
        {
            newColor.a = 0.7f;
        }

        _meshRenderer.material.color = newColor;
        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        _sphereCollider.excludeLayers = excludedLayer;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "ColorWall")
        {
            WallController wall = collision.collider.gameObject.GetComponent<WallController>();
            if (wall.WallColor.Value == _bulletColor.Value)
            {
                Destroy(collision.gameObject);
            }
            Destroy(gameObject);
            
        }
    }
}
