using UnityEngine;
using UnityEditor;
using System.Collections;
using MCS_Utilities;

using MCS;
using MCS.SERVICES;
using MCS.FOUNDATIONS;
using MCS_Utilities.Morph;

[CustomEditor(typeof(MCSMorphFileWrapper))]
public class MCSMorphEditor : Editor
{


    protected Vector2 scrollDeltaVertsPos;
    protected Vector2 scrollDeltaNormalsPos;

    public override void OnInspectorGUI()
    {
        MCSMorphFileWrapper wrapper = (MCSMorphFileWrapper)target;

        GUILayout.Label("File: " + wrapper.fileName);

        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(wrapper.fileName);
            MorphData morphData = MorphData.ConvertBytesToMorphData(bytes);
            GUILayout.Label("Morph: " + morphData.name);
            GUILayout.Label("Mesh: " + morphData.meshName);

            int i = 0;
            GUILayout.Label("Delta Vertices: " + (morphData.blendshapeData.deltaVertices != null ? morphData.blendshapeData.deltaVertices.Length.ToString() : "null"));
            if (morphData.blendshapeData.deltaVertices != null)
            {
                scrollDeltaVertsPos = EditorGUILayout.BeginScrollView(scrollDeltaVertsPos, GUILayout.Height(400));
                foreach (Vector3 v in morphData.blendshapeData.deltaVertices)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(50));
                    EditorGUILayout.Vector3Field("",v);//, GUILayout.Width(300));
                    EditorGUILayout.EndHorizontal();

                    i++;
                }
                EditorGUILayout.EndScrollView();
            }


            GUILayout.Label("Delta Normals: " + (morphData.blendshapeData.deltaNormals != null ? morphData.blendshapeData.deltaNormals.Length.ToString() : "null"));
            if (morphData.blendshapeData.deltaNormals != null)
            {
                scrollDeltaNormalsPos = EditorGUILayout.BeginScrollView(scrollDeltaNormalsPos, GUILayout.Height(400));
                i = 0;
                foreach (Vector3 v in morphData.blendshapeData.deltaNormals)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Vector3Field(i.ToString(), v);//, GUILayout.Width(300));
                    EditorGUILayout.EndHorizontal();

                    i++;
                }
                EditorGUILayout.EndScrollView();
            }

        }
        catch
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            GUILayout.Label("Can't parse morph file, it appears corrupted.");
        }
    }
}



[InitializeOnLoad]
public class MCSMorphFileGlobal
{
    private static MCSMorphFileWrapper wrapper = null;
    private static bool selectionChanged = false;

    static MCSMorphFileGlobal()
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

            for (int i = 0; i < objects.Length; i++)
            {
                newObjects[i] = objects[i];
                instanceIds[i] = objects[i].GetInstanceID();
            }

            string fn = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (fn.ToLower().EndsWith(".morph"))
            {
                if (wrapper == null)
                {
                    wrapper = ScriptableObject.CreateInstance<MCSMorphFileWrapper>();
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

public class MCSMorphFileWrapper : ScriptableObject
{
    [System.NonSerialized]
    public string fileName; // path is relative to Assets/
}
