using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 상자를 나타내는 클래스
/// </summary>
public class CubeController : NetworkBehaviour, IInteractable
{
    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수
    /// </summary>
    [SerializeField] private ColorType _initColor;
    public bool _isFragile = false;


    /// <summary>
    /// 상자의 현재 색깔
    /// </summary>
    private NetworkVariable<ColorType> _cubeColor = new NetworkVariable<ColorType>();
    public NetworkVariable<ColorType> CubeColor
    {
        get => _cubeColor;
    }

    /// <summary>
    /// 상자를 들고 있는 플레이어. 아무도 들고 있지 않으면 NULL이다.
    /// </summary>
    private PlayerController _holdingPlayer;
    public PlayerController HoldingPlayer
    {
        get => _holdingPlayer;
        set => _holdingPlayer = value;
    }

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        if (_initColor == ColorType.None)
        {
            _initColor = (ColorType)Random.Range(1,3);
        }
    }

    public override void OnNetworkSpawn()
    {
        _cubeColor.Value = _initColor;

        _meshRenderer = GetComponent<MeshRenderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();

        // 큐브의 색깔이 변하면 함수 호출하도록 지정
        _cubeColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnCubeColorChanged(before, after);
        };

        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _cubeColor.OnValueChanged.Invoke(_cubeColor.Value, _cubeColor.Value);
            });
        }
        else
        {
            _cubeColor.OnValueChanged.Invoke(_cubeColor.Value, _cubeColor.Value);
        }
    }

    /// <summary>
    /// 상자와 상호작용을 시작한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    public bool StartInteraction(PlayerController player)
    {
        // 이미 다른 사람이 들고 있는 상자이거나, 색깔이 일치하지 않으면 무시
        if (_holdingPlayer != null || _cubeColor.Value != player.PlayerColor.Value)
        {
            return false;
        }

        // 서버 단에서 상호 작용 상태 갱신
        AddHoldingPlayerServerRpc(player.NetworkObject);

        return true;
    }

    /// <summary>
    /// 상자와 상호작용을 중단한다.
    /// </summary>
    public bool StopInteraction(PlayerController player)
    {
        // 서버 단에서 상호 작용 상태 갱신
        RemoveHoldingPlayerServerRpc();

        return true;
    }

    private void Update()
    {
        // 상자의 위치는 Owner에 의해서만 갱신되도록 한다 (서버 또는 들고 있는 사람)
        if (!IsOwner)
        {
            return;
        }

        // 상자를 든 플레이어 앞으로 이동
        if (_holdingPlayer != null)
        {
            Vector3 target = _holdingPlayer.MainCamera.transform.position + _holdingPlayer.MainCamera.transform.forward * 3;
            _rigidbody.velocity = (target - transform.position) * 10;
        }
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
        _boxCollider.excludeLayers = excludedLayer;

        // 누군가 들고 있는 상태에서 색깔이 변한 경우, 강제로 놓는다
        if (IsServer && _holdingPlayer != null)
        {
            RemoveHoldingPlayerServerRpc();
        }
    }

    /// <summary>
    /// 서버 단에서 상자와 상호작용을 시작한다. Ownership을 넘기고 모든 클라이언트에 정보를 전달한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ServerRpc(RequireOwnership = false)]
    private void AddHoldingPlayerServerRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            _rigidbody.useGravity = false;

            // 플레이어가 상자를 조작할 수 있도록 Ownership 변경
            GetComponent<NetworkObject>().ChangeOwnership(playerNetworkObject.OwnerClientId);

            AddHoldingPlayerClientRpc(player);
        }
    }

    /// <summary>
    /// 클라이언트 단에서 상자와 상호작용을 시작한다. 관련 변수를 갱신한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ClientRpc]
    private void AddHoldingPlayerClientRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            _holdingPlayer = playerNetworkObject.gameObject.GetComponent<PlayerController>();
            _holdingPlayer.InteractableInHand = this;
        }
    }

    /// <summary>
    /// 서버 단에서 상자와 상호작용을 중단한다. Ownership을 넘기고 모든 클라이언트에 정보를 전달한다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RemoveHoldingPlayerServerRpc()
    {
        GetComponent<NetworkObject>().RemoveOwnership();
        _rigidbody.useGravity = true;

        RemoveHoldingPlayerClientRpc();
    }

    /// <summary>
    /// 클라이언트 단에서 상자와 상호작용을 중단한다. 관련 변수를 갱신한다.
    /// </summary>
    [ClientRpc]
    private void RemoveHoldingPlayerClientRpc()
    {
        _holdingPlayer.InteractableInHand = null;
        _holdingPlayer = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Owner(서버)에 의해서만 갱신되도록 한다
        if (!IsOwner)
        {
            return;
        }
        if (collision.collider.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.collider.gameObject.GetComponent<PlayerController>();
            if (player.PlayerColor.Value == _cubeColor.Value)
            {
                if (_isFragile)
                {
                    Destroy(gameObject);
                }
            }

        }

    }
}
