using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 멀티플레이어 테스트 UI를 지원하는 싱글톤 클래스
/// </summary>
public class NetworkUI : MonoBehaviour
{
    public static NetworkUI Instance;

    [SerializeField] Button _hostButton;
    [SerializeField] Button _clientButton;
    [SerializeField] TMP_Text _yourColorText;
    private WallSpawner wallSpawner;

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        wallSpawner = FindObjectOfType<WallSpawner>(); // WallSpawner 인스턴스 가져오기

        _hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            
            if (wallSpawner != null)
            {
                StartCoroutine(wallSpawner.SpawnWall()); // WallSpawner의 코루틴 호출
            }
            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(false);
        }
        );
        _clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();

            _hostButton.gameObject.SetActive(false);
            _clientButton.gameObject.SetActive(false);
        }
        );
    }

    public void UpdateYourColorText(ColorType newColor)
    {
        if (newColor == ColorType.Red) _yourColorText.text = "Your Color: <color=\"red\">Red</color>";
        else _yourColorText.text = "Your Color: <color=\"blue\">Blue</color>";
    }
}
