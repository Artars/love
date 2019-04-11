using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitPointer : MonoBehaviour
{

    public enum Role {Pilot, Gunnerr}

    [Header("Objetos")]
    public GameObject hitPointer;

    [Header("Parâmetros")]
    public float fadeSpeed;

    [HideInInspector]public Image pointerImage;
    [HideInInspector]public float hitAngle;//Ângulo que irá apontar. deve ser definido na função que criar este objeto.
    [HideInInspector]public Transform cameraTransform;//Transform da câmera. Definir ao criar objeto.

    void UpdateFading()
    {
        Color tmp = pointerImage.color;
        tmp.a -= fadeSpeed * Time.deltaTime;

        if (tmp.a <= 0)
        {
            Destroy(gameObject);
        }

        pointerImage.color = tmp;
    }

    void UpdateAngle()
    {
        hitPointer.transform.eulerAngles = new Vector3(0, 0, cameraTransform.eulerAngles.y + hitAngle + hitAngle);//Corrigir depois
    }

    void Start()
    {
        pointerImage = hitPointer.GetComponent<Image>();
    }

    void Update()
    {
        UpdateFading();
        UpdateAngle();
    }
}
