using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ClearTrigger : MonoBehaviour
{
    private BoxCollider _boxCollider;

    private void OnEnable()
    {
        // 에디터 모드에서도 BoxCollider를 가져옴
        if (!Application.isPlaying)
        {
            _boxCollider = GetComponent<BoxCollider>();
        }
    }
    void Update()
    {

    }
    private void OnDrawGizmos()
    {
        // 에디터 모드와 플레이 모드 모두에서 BoxCollider 크기를 사용
        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                Debug.LogWarning("BoxCollider is missing, cannot draw Gizmos.");
                return;
            }
        }

        Gizmos.color = Color.green; // Gizmos 색상 설정
        Gizmos.DrawWireCube(transform.position, Vector3.Scale(_boxCollider.size, transform.localScale));
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
