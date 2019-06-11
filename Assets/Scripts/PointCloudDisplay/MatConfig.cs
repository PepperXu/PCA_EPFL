using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class MatConfig : MonoBehaviour {

    // Use this for initialization
    private Material material;
    public static bool useInterpolation = true;
    public static bool useAdaptivePointSize = true;
    public static float pointSize = 0.2f;
    public static byte[] tint = { 255, 255, 255 };

    void OnEnable () {
        LoadShaders();
    }
    
    void LoadShaders()
    {
        
        material = GetComponent<MeshRenderer>().sharedMaterial;
        if (material == null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name);
            
            material = GetComponent<MeshRenderer>().sharedMaterial;
            if (material == null)
            {
                Debug.LogWarning("Material not found. Please reimport point cloud or change material property manually.");
            }
            else
            {
                material.SetFloat("_PointSizeScale", transform.localScale.x);
                material.SetFloat("_PointSize", pointSize);
                material.SetInt("_UseInterpolation", useInterpolation ? 1 : 0);
                Color c = new Color32(tint[0], tint[1], tint[2], 255);
                material.SetColor("_Tint", c);
            }
        }
    }

    private void Update()
    {
        LoadShaders();
    }

}
