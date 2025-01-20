using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;
using System.Data;
using UnityEngine.UIElements;

public class WallSpawner : NetworkBehaviour
{
    [SerializeField] private Transform spawnedObjectPrefab;
    [SerializeField] private float _spawnDuration;

    // Start is called before the first frame update
    void Awake()
    {
        if (_spawnDuration<=1) _spawnDuration=1f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator SpawnWall()
    {
        while(true) 
        {
            Vector3 leftSpawnPosition=transform.position+new Vector3(-4,0,0);
            Vector3 rightSpawnPosition=transform.position+new Vector3(4,0,0);
            SideType spawnSide = (SideType)Random.Range(0,2);
            Transform spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
            spawnedObjectTransform.GetComponent<Rigidbody>().isKinematic = false;   // true로 바꾸고 translate쓰게 ㄱㄱ

            if (spawnSide==SideType.Left)
            {
                spawnedObjectTransform.position = leftSpawnPosition;
                
            }
            else
            {
                spawnedObjectTransform.position = rightSpawnPosition;
            }
            GameManager.instance.enqueueWall(spawnSide, spawnedObjectTransform.gameObject);
            yield return new WaitForSeconds(_spawnDuration);
        }
        
        // Transform _plocarr = MultiplayerManager.Instance.LocalPlayer.transform
        // _gma = MultiplayerManager.Instance.LocalPlayer.PlayerColor.Value

        // _targetTransform = 
    }
}
