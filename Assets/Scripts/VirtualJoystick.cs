using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour
{
    public static VirtualJoystick instance;

    public Image joyBase;
    public Image joyMove;

    protected Vector2 input;


    protected void Awake(){
        if(instance == null) {
            instance = this;
        }
        else if(instance != this) {
            Destroy(gameObject);
        }
    }

    void Update() {
        //If it's cliking
        if(Input.GetMouseButton(0)){
            Vector2 mousePosition = Input.mousePosition;
            if(isInsideRange(mousePosition)){
                // RectTransformUtility
                Vector2 rectPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(joyBase.rectTransform,mousePosition,Camera.current, out rectPos);
                
                rectPos.x = (rectPos.x / joyBase.rectTransform.sizeDelta.x);
                rectPos.y = (rectPos.y / joyBase.rectTransform.sizeDelta.y);
                
                Debug.Log("Rect Pos: "+ rectPos);
                // joyMove.rectTransform.localPosition = rectPos;

                // Vector3 posOffset = mousePosition - joyBase.rectTransform.rect.center;
                // joyMove.rectTransform.localPosition = posOffset;
                // posOffset.Normalize();
                // input.x = posOffset.x;
                // input.y = posOffset.y;
                // Debug.Log("Input: " + input);
            }
        }
        else {
            joyMove.rectTransform.localPosition = Vector2.zero;
            input = Vector2.zero;
        }
    }

    public Vector2 getInput(){
        return input;
    }

    protected bool isInsideRange(Vector2 pos) {
        return RectTransformUtility.RectangleContainsScreenPoint(joyBase.rectTransform,pos);
    }

}
