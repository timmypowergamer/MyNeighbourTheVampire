// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEditor;
using UnityEngine;

namespace Fungus.EditorUtils
{
    [CustomEditor (typeof(Character))]
    public class CharacterEditor : ReorderableArrayEditor
    {
        protected SerializedProperty nameTextProp;
        protected SerializedProperty nameColorProp;
        protected SerializedProperty soundEffectProp;
        protected SerializedProperty portraitsProp;
        protected SerializedProperty portraitsFaceProp;
        protected SerializedProperty descriptionProp;
		protected SerializedProperty graphicProp;
		protected SerializedProperty backgroundProp;
		protected SerializedProperty iconProp;
		protected SerializedProperty profileProp;

        protected virtual void OnEnable()
        {
            nameTextProp = serializedObject.FindProperty ("nameText");
            nameColorProp = serializedObject.FindProperty ("nameColor");
            soundEffectProp = serializedObject.FindProperty ("soundEffect");
            portraitsProp = serializedObject.FindProperty ("portraits");
            portraitsFaceProp = serializedObject.FindProperty ("portraitsFace");
            descriptionProp = serializedObject.FindProperty ("description");
			graphicProp = serializedObject.FindProperty("graphicPrefab");
			backgroundProp = serializedObject.FindProperty("backgroundImage");
			profileProp = serializedObject.FindProperty("profileSprite");
			iconProp = serializedObject.FindProperty("icon");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Character t = target as Character;

            EditorGUILayout.PropertyField(nameTextProp, new GUIContent("Name Text", "Name of the character display in the dialog"));
            EditorGUILayout.PropertyField(nameColorProp, new GUIContent("Name Color", "Color of name text display in the dialog"));
            EditorGUILayout.PropertyField(soundEffectProp, new GUIContent("Sound Effect", "Sound to play when the character is talking. Overrides the setting in the Dialog."));
			EditorGUILayout.PropertyField(graphicProp);
			EditorGUILayout.PropertyField(backgroundProp);
			EditorGUILayout.PropertyField(iconProp);
			EditorGUILayout.PropertyField(profileProp);
			EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description", "Notes about this story character (personality, attibutes, etc.)"));

            if (t.ProfileSprite != null)
            {
                Texture2D characterTexture = t.ProfileSprite.texture;
                float aspect = (float)characterTexture.width / (float)characterTexture.height;
                Rect previewRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (characterTexture != null)
                    GUI.DrawTexture(previewRect,characterTexture,ScaleMode.ScaleToFit,true,aspect);
            }

            DrawReorderableArray(portraitsProp);

            EditorGUILayout.HelpBox("All portrait images should use the exact same resolution to avoid positioning and tiling issues.", MessageType.Info);

            EditorGUILayout.Separator();

            string[] facingArrows = new string[]
            {
                "FRONT",
                "<--",
                "-->",
            };
            portraitsFaceProp.enumValueIndex = EditorGUILayout.Popup("Portraits Face", (int)portraitsFaceProp.enumValueIndex, facingArrows);

            EditorGUILayout.Separator();

            EditorUtility.SetDirty(t);

            serializedObject.ApplyModifiedProperties();
        }

    }
}