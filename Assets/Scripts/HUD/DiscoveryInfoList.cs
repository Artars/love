using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class DiscoveryInfoList : MonoBehaviour
{
    public Transform parentElement;
    public GameObject buttonPrefab;

    protected List<float> lastReceivedTime;
    protected List<DiscoveryInfo> discoveryInfos;
    protected List<Button> buttons;
    protected Coroutine cleanCoroutine = null;

    void Start()
    {
        discoveryInfos = new List<DiscoveryInfo>();
        buttons = new List<Button>();
        lastReceivedTime = new List<float>();
        NetworkDiscovery.onReceivedServerResponse += OnReceivedServerResponse;
    }

    protected void OnReceivedServerResponse(DiscoveryInfo info)
    {
        //Start coroutine to clean
        if(cleanCoroutine == null)
            cleanCoroutine = StartCoroutine(CleanOptions(NetworkDiscovery.instance.ActiveDiscoverySecondInterval+1));

        int id = FindInfoInList(info);

        if(id != -1)
        {
            lastReceivedTime[id] = Time.deltaTime;
        }
        else
        {
            GameObject newButton = GameObject.Instantiate(buttonPrefab, parentElement);
            newButton.SetActive(true);
            Button buttonScript = newButton.GetComponent<Button>();
            TMPro.TMP_Text text = newButton.GetComponentInChildren<TMPro.TMP_Text>();

            buttons.Add(buttonScript);
            int size = buttons.Count - 1;
            buttonScript.onClick.AddListener(() => OnClickJoin(size));

            discoveryInfos.Add(info);
            if(text != null)
            {
                text.text = info.unpackedData.hostName;
            }

            lastReceivedTime.Add(Time.time);
        }
    }

    protected int FindInfoInList(DiscoveryInfo info)
    {
        string GUID = info.unpackedData.serverGUID;
        for (int i = 0; i < discoveryInfos.Count; i++)
        {
            if(GUID == discoveryInfos[i].unpackedData.serverGUID)
            {
                return i;
            }
        }

        return -1;
    }

    public void OnClickJoin(int id)
    {
        DiscoveryInfo info = discoveryInfos[id];

        NetworkManager.singleton.networkAddress = info.EndPoint.Address.MapToIPv4().ToString();
        ((TelepathyTransport)Transport.activeTransport).port = (ushort)info.unpackedData.port;

        if(cleanCoroutine != null)
            StopCoroutine(cleanCoroutine);

        NetworkDiscovery.instance.StopDiscovery();
        NetworkManager.singleton.StartClient();

    }

    protected IEnumerator CleanOptions(float timeInterval)
    {
        while(true)
        {
            float currentTime = Time.time;
            for (int i = buttons.Count-1; i >-1 ; i--)
            {
                if((currentTime - lastReceivedTime[i]) > timeInterval)
                {
                    Destroy(buttons[i].gameObject);
                    buttons.RemoveAt(i);
                    lastReceivedTime.RemoveAt(i);
                    discoveryInfos.RemoveAt(i);
                }
            }

            yield return new WaitForSecondsRealtime(timeInterval);
        }
    }
}
