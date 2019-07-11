using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkHud : MonoBehaviour
{
    public InputField adressField;
    public MapCollection mapCollection;

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
        NetworkManager.singleton.onlineScene = mapCollection.mapOptions[0].scene;
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
