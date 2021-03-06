﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Discovery;

public class NetworkHud : MonoBehaviour
{
    public TankOptionCollection tankOptionCollection;
    public InputField adressField;
    public InputField portField;
    public MapSelector mapSelector;
    public SettingsSelector settingsSelector;
    public Animator animator;

    public TMPro.TMP_Text connectionStatus;
    public TMPro.TMP_Text connectButtonText;

    protected int currentMenu = 0;

    public string address = "localhost";
    public string port = "7777";
    public string sceneToChange;

    protected NetworkManager networkManager;
    bool tryingToConnect = false;


    public void Start(){
        if(PlayerPrefs.HasKey("LastAddress")){
            address = PlayerPrefs.GetString("LastAddress");
            port = PlayerPrefs.GetString("LastPort", "7777");
            if(adressField != null){
                adressField.text = address;
            }
            portField.text = port;
        }

        networkManager = NetworkManager.singleton;
    }

    public void Update()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                if (!NetworkClient.active)
                {
                    connectionStatus.text = "";
                    connectButtonText.text = "Connect";
                    tryingToConnect = false;
                }
                else
                {
                    // Connecting
                    connectionStatus.text = "Connecting to " + networkManager.networkAddress;
                    connectButtonText.text = "Cancel";
                    tryingToConnect = true;
                }
            }
    }


    
    public void updateAdressField(InputField  input) {
        this.address = input.text;
    }

    public void updatePortField(InputField  input) {
        this.port = input.text;
    }

    public void OnClickStartHost(){
        MapOption map = mapSelector.GetSelectedMapOption();
        settingsSelector.SaveSettings();
        MatchSetting matchSetting = settingsSelector.GetMatchSetting();

        MatchConfiguration.instance.matchSetting = matchSetting;
        MatchConfiguration.instance.mapOption = map;
        NetworkManager.singleton.onlineScene = map.scene;

        //Make tank settings
        List<InfoTank> tankInfo = new List<InfoTank>();
        int index = 0;
        for (int i = 0; i < matchSetting.teamConfiguration.Length; i++)
        {
            for (int j = 0; j < matchSetting.teamConfiguration[i]; j++)
            {
                InfoTank tank = new InfoTank(index,i, tankOptionCollection.tankOptions[i % tankOptionCollection.tankOptions.Length]);
                tankInfo.Add(tank);

                index++;
            }
        }

        //Change port of the server
        TelepathyTransport transport = NetworkManager.singleton.gameObject.GetComponent<TelepathyTransport>();
        Mirror.Websocket.WebsocketTransport websocketTransport = NetworkManager.singleton.gameObject.GetComponent<Mirror.Websocket.WebsocketTransport>();
        if(transport != null)
        {
            transport.port = matchSetting.serverPort;
        }
        else if (websocketTransport != null)
        {
            websocketTransport.port = matchSetting.serverPort;
        }
        else
        {
            Mirror.LiteNetLib4Mirror.LiteNetLib4MirrorTransport liteTransport = 
            NetworkManager.singleton.gameObject.GetComponent<Mirror.LiteNetLib4Mirror.LiteNetLib4MirrorTransport>();

            if(liteTransport != null)
            {
                liteTransport.port = matchSetting.serverPort;
            }
        }

        MatchConfiguration.instance.infoTanks = tankInfo;

        Mirror.Discovery.NetworkDiscovery discovery = NetworkManager.singleton.GetComponent<Mirror.Discovery.NetworkDiscovery>();
        if(discovery != null)
            discovery.StopDiscovery();

        if(NetworkClient.active)
            NetworkManager.singleton.StopClient();
        NetworkManager.singleton.StartHost();

        // NetworkDiscovery.instance.ServerPassiveBroadcastGame(CreateServerInformation());
        NetworkDiscovery networkDiscovery = NetworkManager.singleton.GetComponent<NetworkDiscovery>();
        if(networkDiscovery != null)
            networkDiscovery.AdvertiseServer();
    }

    public void OnServerStart(){
    }



    public void OnClickStartClient(){
        if(!tryingToConnect)
        {
            PlayerPrefs.SetString("LastAddress", address);
            PlayerPrefs.SetString("LastPort", port);
            NetworkManager.singleton.networkAddress = address;
            TelepathyTransport telTransport = NetworkManager.singleton.gameObject.GetComponent<TelepathyTransport>();
            if(telTransport != null)
                telTransport.port = ushort.Parse(port);
            else
            {
                Mirror.LiteNetLib4Mirror.LiteNetLib4MirrorTransport liteTransport = 
                NetworkManager.singleton.gameObject.GetComponent<Mirror.LiteNetLib4Mirror.LiteNetLib4MirrorTransport>();

                if(liteTransport != null)
                {
                    liteTransport.port = ushort.Parse(port);
                }
            }
            NetworkManager.singleton.StartClient();
        }
        else
        {
            networkManager.StopClient();
        }
    }

    public void ChangeMenu(int newMenu)
    {
        if(animator != null)
        {
            currentMenu = newMenu;
            animator.SetInteger("CurrentMenu", currentMenu);
        }
    }

    public void CloseGame(float timeToClose)
    {
        StartCoroutine(WaitToClose(timeToClose));
    }

    protected IEnumerator WaitToClose(float time)
    {
        float counter = 0;

        while(counter < time)
        {
            counter += Time.deltaTime;
            yield return null;
        }

        Application.Quit();
    }

}
