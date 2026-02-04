using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//AI CODE!
public class MeshCombinerWindow : EditorWindow
{
    public List<Mesh> meshesToCombine = new List<Mesh>();
    public Mesh selectedMesh = null;
    public GameObject selectedModel = null;
    private Vector2 scrollPos;
    private Dictionary<Mesh, Transform> meshToTransform = new Dictionary<Mesh, Transform>();

    [MenuItem("Rowen/Mesh Combiner")]
    private static void Init()
    {
        GetWindow<MeshCombinerWindow>();
    }

    private void OnGUI()
    {
        using(new GUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Combine Selected Meshes"))
            {
                CombineAllSelectedMeshes();
            }
            
            if(GUILayout.Button("Clear List"))
            {
                meshesToCombine.Clear();
            }
        }   
        
        DrawMeshes();
    }

    private void CombineAllSelectedMeshes()
    {
        Mesh mesh = new Mesh();
        foreach (var combine in meshesToCombine)
        {
            var transform = meshToTransform[combine];
            mesh = CombineMeshes(mesh, combine, transform);
        }
        // Populate your mesh here (vertices, triangles, etc.)
        string path = EditorUtility.SaveFilePanel(
                    "Save Mesh Asset",
                    "Assets",
                    "NewMesh",
                    "asset"
                );

        if (!string.IsNullOrEmpty(path))
        {
            // Convert absolute path to relative project path
            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

            AssetDatabase.CreateAsset(mesh, relativePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Mesh saved to " + relativePath);

        }
    }

    private void DrawMeshes()
    {
         // ObjectField for selecting a model prefab / FBX / imported asset
        selectedModel = (GameObject)EditorGUILayout.ObjectField(
            "Model",
            selectedModel,
            typeof(GameObject),
            false
        );

        if (selectedModel != null && GUILayout.Button("Harvest Meshes"))
        {
            HarvestMeshesFromModel(selectedModel);
        }

        // ObjectField for a single mesh
        selectedMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedMesh, typeof(Mesh), false);

        // Add button
        if (selectedMesh != null)
        {
            if (GUILayout.Button("Add to List"))
            {
                if (!meshesToCombine.Contains(selectedMesh))
                    meshesToCombine.Add(selectedMesh);

                selectedMesh = null; // clear field
            }
        }

        GUILayout.Space(10);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.Label("Mesh List", EditorStyles.boldLabel);

        // Render the list
        foreach (var m in meshesToCombine)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(m ? m.name : "<null>");
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }
    private static Mesh CombineMeshes(Mesh a, Mesh b, Transform transformB)
    {
        Mesh combined = new Mesh();

        // --- Vertices ---
        Vector3[] vertsA = a.vertices;
        Vector3[] vertsB = b.vertices;

        Vector3[] verts = new Vector3[vertsA.Length + vertsB.Length];
        vertsA.CopyTo(verts, 0);

        for(int i = 0 ; i < vertsB.Length; i++)
        {
            var vert = vertsB[i];
            Vector3 transformedVert = vert;
            if(transformB != null)
            {
                transformedVert = transformB.TransformPoint(vert);
            }
            verts[vertsA.Length + i] = transformedVert;
        }

        // --- Triangles ---
        int[] trisA = a.triangles;
        int[] trisB = b.triangles;

        int[] tris = new int[trisA.Length + trisB.Length];
        trisA.CopyTo(tris, 0);

        // Offset B’s triangle indices by A’s vertex count
        for (int i = 0; i < trisB.Length; i++)
            tris[i + trisA.Length] = trisB[i] + vertsA.Length;

        // --- Normals ---
        Vector3[] normsA = a.normals;
        Vector3[] normsB = b.normals;

        Vector3[] norms = new Vector3[verts.Length];
        normsA.CopyTo(norms, 0);
        normsB.CopyTo(norms, normsA.Length);

        // --- UVs ---
        Vector2[] uvsA = a.uv;
        Vector2[] uvsB = b.uv;

        Vector2[] uvs = new Vector2[verts.Length];
        uvsA.CopyTo(uvs, 0);
        uvsB.CopyTo(uvs, uvsA.Length);

        // --- Assign to mesh ---
        combined.vertices = verts;
        combined.triangles = tris;
        combined.normals = norms;
        combined.uv = uvs;

        combined.RecalculateBounds();
        return combined;
    }
    void HarvestMeshesFromModel(GameObject root)
    {
        // Get all MeshFilters
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh != null && !meshesToCombine.Contains(mf.sharedMesh))
            {
                meshesToCombine.Add(mf.sharedMesh);
                meshToTransform[mf.sharedMesh] = mf.transform;
            }
        }

        // Get all SkinnedMeshRenderers
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh != null && !meshesToCombine.Contains(smr.sharedMesh))
            {
                meshesToCombine.Add(smr.sharedMesh);
                meshToTransform[smr.sharedMesh] = smr.transform;

            }
        }
    }

}
