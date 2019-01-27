// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEditor;
using UnityEngine;

namespace Fungus.EditorUtils
{
    [CustomEditor (typeof(WriterAudio))]
    public class WriterAudioEditor : Editor
    {
        protected SerializedProperty volumeProp;
        protected SerializedProperty pitchLoProp;
        protected SerializedProperty pitchHiProp; 
        protected SerializedProperty loopProp;
        protected SerializedProperty targetAudioSourceProp;
        protected SerializedProperty audioModeProp;
        protected SerializedProperty beepSoundsProp;
		protected SerializedProperty boopSoundsProp;
		protected SerializedProperty soundEffectProp;
        protected SerializedProperty inputSoundProp;

        protected virtual void OnEnable()
        {
            volumeProp = serializedObject.FindProperty("volume");
            loopProp = serializedObject.FindProperty("loop");
            targetAudioSourceProp = serializedObject.FindProperty("targetAudioSource");
            inputSoundProp = serializedObject.FindProperty("inputSound");
            audioModeProp = serializedObject.FindProperty("audioMode");
            beepSoundsProp = serializedObject.FindProperty("beepSounds");
			boopSoundsProp = serializedObject.FindProperty("boopSounds");
			soundEffectProp = serializedObject.FindProperty("soundEffect");
            pitchLoProp = serializedObject.FindProperty("pitchLo");
            pitchHiProp = serializedObject.FindProperty("pitchHi");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(volumeProp);
            EditorGUILayout.PropertyField(pitchLoProp);
            EditorGUILayout.PropertyField(pitchHiProp);
            EditorGUILayout.PropertyField(loopProp);
            EditorGUILayout.PropertyField(targetAudioSourceProp);
            EditorGUILayout.PropertyField(inputSoundProp);

            EditorGUILayout.PropertyField(audioModeProp);
            if ((AudioMode)audioModeProp.enumValueIndex == AudioMode.Beeps)
            {
                ReorderableArrayEditor.DrawReorderableArray(beepSoundsProp);
            }
			else if ((AudioMode)audioModeProp.enumValueIndex == AudioMode.Boopy)
			{
				ReorderableArrayEditor.DrawReorderableArray(boopSoundsProp);
			}
            else
            {
                EditorGUILayout.PropertyField(soundEffectProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}