using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitmark : MonoBehaviour
{

    public float timeToDestroy = 2;
    public float distanceSizeRatio = 0.01f;
    public float distanceToObject = 3;
    public Transform spriteTransform;
    public AnimationCurve spriteSizeAnimation;


    protected Transform cameraTransform;
    protected Transform myTransform;
    // Start is called before the first frame update
    void Start()
    {
        cameraTransform = Camera.main.transform;
        myTransform = transform;
        spriteTransform.localPosition = new Vector3(0,0,distanceToObject);
        Destroy(gameObject, timeToDestroy);
        StartCoroutine(GrowAnimation());
    }

    public void Update()
    {
        myTransform.LookAt(cameraTransform, Vector3.up);
        Vector3 dif = cameraTransform.position - myTransform.position;
        float newScale = 1 + (dif.magnitude-distanceToObject) * distanceSizeRatio;
        myTransform.localScale = new Vector3(newScale,newScale,newScale);
    }

    protected IEnumerator GrowAnimation()
    {
        float counter = 0, porcent = 0;

        do
        {
            float scale = spriteSizeAnimation.Evaluate(porcent);
            spriteTransform.localScale = new Vector3(scale, scale, scale);
            counter += Time.deltaTime;
            porcent = counter/timeToDestroy;
            yield return null;
        }
        while(counter < timeToDestroy);
    }


}
