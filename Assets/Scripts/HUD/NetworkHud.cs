using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkHud : MonoBehaviour
{
    public TankOptionCollection tankOptionCollection;
    public InputField adressField;
    public InputField portField;
    public MapSelector mapSelector;
    public SettingsSelector settingsSelector;
    public Animator animator;

    protected int currentMenu = 0;

    public string address = "localhost";
    public string port = "7777";
    public string sceneToChange;


    public void Start(){
        if(PlayerPrefs.HasKey("LastAddress")){
            address = PlayerPrefs.GetString("LastAddress");
            port = PlayerPrefs.GetString("LastPort", "7777");
            if(adressField != null){
                adressField.text = address;
            }
            portField.text = port;
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
        if(transport != null)
        {
            transport.port = matchSetting.serverPort;
        }

        MatchConfiguration.instance.infoTanks = tankInfo;

        NetworkDiscovery.instance.ServerPassiveBroadcastGame(CreateServerInformation());

        NetworkManager.singleton.StartHost();
    }

    protected byte[] CreateServerInformation()
    {
        // Wire in broadcaster pipeline here
        Assets.Scripts.NetworkMessages.GameBroadcastPacket gameBroadcastPacket = new Assets.Scripts.NetworkMessages.GameBroadcastPacket();

        gameBroadcastPacket.serverAddress = NetworkManager.singleton.networkAddress;
        gameBroadcastPacket.port = ((TelepathyTransport)Transport.activeTransport).port;
        gameBroadcastPacket.hostName = PlayerPrefs.GetString("Name", "Dummy");
        gameBroadcastPacket.serverGUID = NetworkDiscovery.instance.serverId;

        byte[] broadcastData = Assets.Scripts.Utility.Serialisation.ByteStreamer.StreamToBytes(gameBroadcastPacket);
        // NetworkDiscovery.instance.ServerPassiveBroadcastGame(broadcastData);

        return broadcastData;
    }

    public void OnServerStart(){
    }



    public void OnClickStartClient(){
        PlayerPrefs.SetString("LastAddress", address);
        PlayerPrefs.SetString("LastPort", port);
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.gameObject.GetComponent<TelepathyTransport>().port = ushort.Parse(port);
        NetworkManager.singleton.StartClient();
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
