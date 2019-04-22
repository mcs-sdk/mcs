using UnityEngine;
using UnityEditor;
using System.Collections;
using MCS_Utilities;

[CustomEditor(typeof(MCSResourceFileWrapper))]
public class MCSResourceEditor : Editor {
    public override void OnInspectorGUI()
    {
        MCSResourceFileWrapper wrapper = (MCSResourceFileWrapper)target;

        GUILayout.Label(wrapper.fileName);

        try
        {
            MCSResource mr = new MCSResource();
            mr.Read(wrapper.fileName);
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("" + mr.header.Keys.Length + " entries");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File",GUILayout.Width(250));
                EditorGUILayout.LabelField("Size",GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < mr.header.Keys.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(mr.header.Keys[i],GUILayout.Width(250));
                float kB = ((float)mr.header.Lengths[i]) / 1024f;
                EditorGUILayout.TextField(kB.ToString("F2") + "kB",GUILayout.Width(100));
                bool export = GUILayout.Button("Export",GUILayout.Width(100));
                if (export)
                {
                    mr.UnpackResource(mr.header.Keys[i]);
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }

        } catch
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            GUILayout.Label("Can't parse resource file, it appears corrupted.");
        }
    }
}



[InitializeOnLoad]
public class MCSResourceFileGlobal
{
    private static MCSResourceFileWrapper wrapper = null;
    private static bool selectionChanged = false;

    static MCSResourceFileGlobal()
    {
        Selection.selectionChanged += SelectionChanged;
        EditorApplication.update += Update;
    }

    private static void SelectionChanged()
    {
        selectionChanged = true;
        // can't do the wrapper stuff here. it does not work 
        // when you Selection.activeObject = wrapper
        // so do it in Update
    }

    private static void Update()
    {
        if (selectionChanged == false) return;

        selectionChanged = false;
        if (Selection.activeObject != wrapper)
        {
            if (Selection.objects.Length > 1)
            {
                //they have multiple files selected
                return;
            }

            Object[] objects = Selection.objects;
            int[] instanceIds = new int[objects.Length + 1];
            Object[] newObjects = new Object[objects.Length + 1];

            for(int i = 0; i < objects.Length; i++)
            {
                newObjects[i] = objects[i];
                instanceIds[i] = objects[i].GetInstanceID();
            }

            string fn = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (fn.ToLower().EndsWith(".mr"))
            {
                if (wrapper == null)
                {
                    wrapper = ScriptableObject.CreateInstance<MCSResourceFileWrapper>();
                    wrapper.hideFlags = HideFlags.DontSave;
                }
                newObjects[objects.Length] = wrapper;
                instanceIds[objects.Length] = wrapper.GetInstanceID();

                wrapper.fileName = fn;
                Selection.activeObject = wrapper;
            }
        }
    }
}

// M3DResourceFileWrapper.cs 
public class MCSResourceFileWrapper : ScriptableObject
{
    [System.NonSerialized]
    public string fileName; // path is relative to Assets/
}
