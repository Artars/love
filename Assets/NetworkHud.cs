using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Components;

public class NetworkHud : MonoBehaviour
{
    public string address = "localhost";
    public string sceneToChange;

    
    public void updateAdressField(InputField  input) {
        this.address = input.text;
    }

    public void OnClickStartHost(){
        NetworkingManager.singleton.OnServerStarted += OnServerStart;
        NetworkingManager.singleton.StartHost();
    }

    public void OnServerStart(){
        NetworkSceneManager.SwitchScene(sceneToChange);
    }



    public void OnClickStartClient(){
        NetworkingManager.singleton.NetworkConfig.ConnectAddress = address;
        NetworkingManager.singleton.StartClient();
    }

}
