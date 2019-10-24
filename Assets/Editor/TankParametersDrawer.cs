using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(TankParameters))]
public class TankParametersDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight (property) + 20;
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);
        if (property.isExpanded) {
            if(GUI.Button(new Rect(position.x,position.y+position.height-20,position.width,20), "Fix gear values"))
            {
                TankParameters tankParameters = (TankParameters) fieldInfo.GetValue(property.serializedObject.targetObject);
                tankParameters.gearSystem.FixValueArray();        
            }
        }
    }
}

#endif