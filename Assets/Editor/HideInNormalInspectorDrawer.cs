 using UnityEngine;
 using UnityEditor;
 
 [CustomPropertyDrawer(typeof(HideInNormalInspectorAttribute))]
 class HideInNormalInspectorDrawer : PropertyDrawer
 {
     public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
         return 0f;
     }
     
     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {}
 }