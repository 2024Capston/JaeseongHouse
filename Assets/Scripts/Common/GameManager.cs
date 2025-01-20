using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;
using System.Data;
using JetBrains.Annotations;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance; // 싱글톤을 할당할 전역 변수

    public bool isGameover = false; // 게임 오버 상태
    public Text scoreText; // 점수를 출력할 UI 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화 할 UI 게임 오브젝트
    public int onButtonCount = 0;
    public bool isCubeReady = true;
    private int score = 0; // 게임 점수
    private bool chasingMode = false;

    private Queue<GameObject> leftComingWallQueue = new Queue<GameObject>();
    private Queue<GameObject> rightComingWallQueue = new Queue<GameObject>();

    [SerializeField] private Transform spawnedObjectPrefab;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Debug.LogWarning("씬에 두개 이상의 게임 매니저가 존재합니다!");
            Destroy(gameObject);
        }


    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"{GameManager.instance.onButtonCount}");

        if (onButtonCount>=2 && isCubeReady)
        {
            isCubeReady=false;
            for (int i=0; i<7; i++)
            {
                Transform spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
                spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
                spawnedObjectTransform.GetComponent<Rigidbody>().isKinematic = false;
                spawnedObjectTransform.position = 
                        new Vector3(Random.Range(-10f, 40f), 5, Random.Range(30f, 90f));
                spawnedObjectTransform.GetComponent<CubeController>()._isFragile = true;
            }
            GameObject targetObject = GameObject.Find("ChasingMonster");
            if (targetObject != null)
            {
                targetObject.GetComponent<ChasingEnemy>().OnChasing();
            }
            else
            {
                Debug.Log("오브젝트를 찾을 수 없거나 이미 활성화 상태입니다.");
            }
            chasingMode = true;
        }

        //Debug.Log("적 오브젝트 개수: " + enemyCount);
        if (chasingMode)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Cube");
            int enemyCount = enemies.Length;
            if (enemyCount<=0)
            {
                GameObject targetObject = GameObject.Find("ChasingMonster");
                if (targetObject != null)
                {
                    targetObject.SetActive(false);  // 오브젝트를 비활성화
                }
            }
        }
        // Debug.Log("L:"+instance.leftComingWallQueue.Count+", R:"+instance.rightComingWallQueue.Count);
    }
    public void enqueueWall(SideType side, GameObject go)
    {
        if (side == SideType.Left)
        {
            instance.leftComingWallQueue.Enqueue(go);
        }
        else
        {
            instance.rightComingWallQueue.Enqueue(go);
        }
    }
    public void destroyWall(SideType side, GameObject go)
    {
        Queue<GameObject> queue = null;
        if (side == SideType.Left)
        {
            queue = instance.leftComingWallQueue;
        }
        else
        {
            queue = instance.rightComingWallQueue;
        }
        if (go == queue.Peek())
        {
            queue.Dequeue();
            Destroy(go);
        }
    }
    public void ShotWall(SideType side, ColorType color)
    {
        Debug.Log("SHot");
        GameObject frontWall=null; 
        Queue<GameObject> wallQueue = null;
        if (side == SideType.Left)
        {
            wallQueue = instance.leftComingWallQueue;
            Debug.Log("get Left que");
        }
        else
        {
            wallQueue = instance.rightComingWallQueue;
            Debug.Log("get Right que");
        }
        if (wallQueue.Count != 0)
        {
            frontWall = wallQueue.Peek();
            Debug.Log("Que is not empty");
        }
        if (frontWall != null && frontWall.GetComponent<WallController>().WallColor.Value == color)
        {
            Debug.Log("Destroy WALL");
            wallQueue.Dequeue();
            Destroy(frontWall);
        }
    }
    public void GameOver()
    {
        Application.Quit();
    }
}
