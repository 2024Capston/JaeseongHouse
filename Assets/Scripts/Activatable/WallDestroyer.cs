using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Runtime.CompilerServices;

/// <summary>
/// 문을 나타내는 클래스.
/// </summary>
public class WallDestroyer : NetworkBehaviour, IActivatable
{
    //[SerializeField] private bool _isOpen;

    [SerializeField] private ColorType _initColor;
    [SerializeField] private SideType _initSide;
    [SerializeField] private float _shotDuration;
    [SerializeField] private float _shotCooltime;

    //[SerializeField] private Animator _animator;

    /// <summary>
    /// 문 열림 시간을 관리하기 위한 보조 변수.
    /// </summary>
    private float _timeTillClose;

    public override void OnNetworkSpawn()
    {
        //if (_isOpen)
        //{
        //    _animator.Play("Open");
        //}
        _shotCooltime = 0;
    }

    public void Update()
    {
        if (!IsServer)
        {
            return;
        }
        _shotCooltime -= Time.deltaTime;
    }

    public bool Activate(PlayerController player)
    {
        DestroyWallServerRpc();
        return true;
    }

    public bool Deactivate(PlayerController player)
    {
        //CloseDoorServerRpc();
        return true;
    }
    public void SetDuration(float duration)
    {
        //_openDuration = duration;
    }

    /// <summary>
    /// 서버 단에서 문을 연다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DestroyWallServerRpc()
    {
        //// 문 여닫기 애니메이션이 진행 중인지 확인
        //bool isPlaying = _animator.GetCurrentAnimatorStateInfo(0).length > _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        if (_shotCooltime<0)
        {
            _shotCooltime = _shotDuration;
            DestroyWallClientRpc();
        }
    }

    /// <summary>
    /// 클라이언트 단에서 문을 연다.
    /// 관련 변수를 각 클라이언트마다 갱신해 준다.
    /// </summary>
    [ClientRpc]
    private void DestroyWallClientRpc()
    {
        GameManager.instance.ShotWall(_initSide, _initColor);
        GetComponent<RenderLine>().DrawLine();
    }
    /// <summary>
    /// 서버 단에서 문을 닫는다.
    /// </summary>
    //[ServerRpc(RequireOwnership = false)]
    //private void CloseDoorServerRpc()
    //{
    //    // 문 여닫기 애니메이션이 진행 중인지 확인
    //    bool isPlaying = _animator.GetCurrentAnimatorStateInfo(0).length > _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

    //    if (_isOpen && !isPlaying)
    //    {
    //        _animator.Play("Close");
    //        CloseDoorClientRpc();
    //    }
    //}

    ///// <summary>
    ///// 클라이언트 단에서 문을 닫는다.
    ///// 관련 변수를 각 클라이언트마다 갱신해 준다.
    ///// </summary>
    //[ClientRpc]
    //private void CloseDoorClientRpc()
    //{
    //    _isOpen = false;
    //}
}