using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkHud : MonoBehaviour
{
    public InputField adressField;
    public MapSelector mapSelector;

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
        MatchConfiguration.instance.mapOption = map;
        NetworkManager.singleton.onlineScene = map.scene;
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
