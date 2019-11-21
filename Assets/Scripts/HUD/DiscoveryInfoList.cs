using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.LiteNetLib4Mirror;

public class DiscoveryInfoList : MonoBehaviour
{
    public Transform parentElement;
    public GameObject buttonPrefab;
    public TMPro.TMP_Text buttonText;

    [SerializeField] public float discoveryInterval = 1f;
    private NetworkManagerHUD _managerHud;
    private bool _noDiscovering = true;

    
    public void OnClickStartDiscovery()
    {
        if(_noDiscovering)
        {
            StartCoroutine(StartDiscovery());
            buttonText.text = "Cancel discovery";
        }
        else
        {
            _noDiscovering = true;
            buttonText.text = "Auto join";
        }
    }

    private IEnumerator StartDiscovery()
    {
        _noDiscovering = false;

        LiteNetLib4MirrorDiscovery.InitializeFinder();
        LiteNetLib4MirrorDiscovery.Singleton.onDiscoveryResponse.AddListener(OnClientDiscoveryResponse);
        while (!_noDiscovering)
        {
            LiteNetLib4MirrorDiscovery.SendDiscoveryRequest("NetworkManagerHUD");
            yield return new WaitForSeconds(discoveryInterval);
        }

        LiteNetLib4MirrorDiscovery.Singleton.onDiscoveryResponse.RemoveListener(OnClientDiscoveryResponse);
        LiteNetLib4MirrorDiscovery.StopDiscovery();
    }

    private void OnClientDiscoveryResponse(System.Net.IPEndPoint endpoint, string text)
    {
        Debug.Log("Discovery: " + text);
        string ip = endpoint.Address.ToString();

        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.maxConnections = 2;
        LiteNetLib4MirrorTransport.Singleton.clientAddress = ip;
        LiteNetLib4MirrorTransport.Singleton.port = (ushort)endpoint.Port;
        LiteNetLib4MirrorTransport.Singleton.maxConnections = 2;
        NetworkManager.singleton.StartClient();
        _noDiscovering = true;
    }

    // protected void OnReceivedServerResponse(DiscoveryInfo info)
    // {
    //     //Start coroutine to clean
    //     if(cleanCoroutine == null)
    //         cleanCoroutine = StartCoroutine(CleanOptions(NetworkDiscovery.instance.ActiveDiscoverySecondInterval+1));

    //     int id = FindInfoInList(info);

    //     if(id != -1)
    //     {
    //         lastReceivedTime[id] = Time.deltaTime;
    //     }
    //     else
    //     {
    //         GameObject newButton = GameObject.Instantiate(buttonPrefab, parentElement);
    //         newButton.SetActive(true);
    //         Button buttonScript = newButton.GetComponent<Button>();
    //         TMPro.TMP_Text text = newButton.GetComponentInChildren<TMPro.TMP_Text>();

    //         buttons.Add(buttonScript);
    //         int size = buttons.Count - 1;
    //         buttonScript.onClick.AddListener(() => OnClickJoin(size));

    //         discoveryInfos.Add(info);
    //         if(text != null)
    //         {
    //             text.text = info.unpackedData.hostName;
    //         }

    //         lastReceivedTime.Add(Time.time);
    //     }
    // }

    // protected int FindInfoInList(DiscoveryInfo info)
    // {
    //     string GUID = info.unpackedData.serverGUID;
    //     for (int i = 0; i < discoveryInfos.Count; i++)
    //     {
    //         if(GUID == discoveryInfos[i].unpackedData.serverGUID)
    //         {
    //             return i;
    //         }
    //     }

    //     return -1;
    // }

    // public void OnClickJoin(int id)
    // {
    //     DiscoveryInfo info = discoveryInfos[id];

    //     NetworkManager.singleton.networkAddress = info.EndPoint.Address.MapToIPv4().ToString();
    //     ((TelepathyTransport)Transport.activeTransport).port = (ushort)info.unpackedData.port;

    //     if(cleanCoroutine != null)
    //         StopCoroutine(cleanCoroutine);

    //     NetworkDiscovery.instance.StopDiscovery();
    //     NetworkManager.singleton.StartClient();

    // }

    // protected IEnumerator CleanOptions(float timeInterval)
    // {
    //     while(true)
    //     {
    //         float currentTime = Time.time;
    //         for (int i = buttons.Count-1; i >-1 ; i--)
    //         {
    //             if((currentTime - lastReceivedTime[i]) > timeInterval)
    //             {
    //                 Destroy(buttons[i].gameObject);
    //                 buttons.RemoveAt(i);
    //                 lastReceivedTime.RemoveAt(i);
    //                 discoveryInfos.RemoveAt(i);
    //             }
    //         }

    //         yield return new WaitForSecondsRealtime(timeInterval);
    //     }
    // }
}
