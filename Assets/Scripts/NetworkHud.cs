using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class NetworkHud : MonoBehaviour
{
    public string address = "localhost";
    public string sceneToChange;

    
    public void updateAdressField(InputField  input) {
        this.address = input.text;
    }

    public void OnClickStartHost(){
        NetworkManager.singleton.StartHost();
    }

    public void OnServerStart(){
    }



    public void OnClickStartClient(){
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }

}
