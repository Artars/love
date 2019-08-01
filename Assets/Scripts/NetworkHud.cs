using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkHud : MonoBehaviour
{
    public TankOptionCollection tankOptionCollection;
    public InputField adressField;
    public MapSelector mapSelector;
    public SettingsSelector settingsSelector;

    public void Start(){
        if(PlayerPrefs.HasKey("LastAddress")){
            address = PlayerPrefs.GetString("LastAddress");
            if(adressField != null){
                adressField.text = address;
            }
        }
    }

    public string address = "localhost";
    public string sceneToChange;

    
    public void updateAdressField(InputField  input) {
        this.address = input.text;
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

        MatchConfiguration.instance.infoTanks = tankInfo;

        NetworkManager.singleton.StartHost();
    }

    public void OnServerStart(){
    }



    public void OnClickStartClient(){
        PlayerPrefs.SetString("LastAddress", address);
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }

}
