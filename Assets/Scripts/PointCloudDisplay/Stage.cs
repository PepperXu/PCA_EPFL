using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Stage : MonoBehaviour {

    // Use this for initialization
    private Bounds bound;
    private GameObject shadowQuad;
    private GameObject shadowStageQuad;
    private GameObject stage;
    public Vector2 quadPos;
    public Vector2 quadScale;
    private bool enableStageShadow = true;
    private float centerHeight = 0.8f;
    private float maxHeight = 1.7f;
    private GameObject[] teleportLocked;

    private ExperimentManager expManager;

    public bool isDistorted = false;


   [System.Serializable]
    public class PointCloudConfig
    {
        public PointCloud[] pointClouds;
    }

    [System.Serializable]
    public class PointCloud
    {
        public float height = 0f;
        public float width = 0f;
        public int useStage = -1;
    }


    private void OnEnable()
    {
        expManager = FindObjectOfType<ExperimentManager>();

        bound = GetComponent<MeshRenderer>().bounds;

        string filePath = Application.dataPath + "/StreamingAssets/" + expManager.configFile + ".json";
        
        PointCloudConfig pcConfig = new PointCloudConfig();
        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            pcConfig = JsonUtility.FromJson<PointCloudConfig>(dataAsJson);
        } else
        {
            Debug.LogError("Config not found!");
            Application.Quit();
        }
        
        
        
        
        if (pcConfig.pointClouds[ExperimentManager.index].height > 0f)
        {
            float scale = (float)pcConfig.pointClouds[ExperimentManager.index].height / (bound.extents.y * 2f);
            transform.localScale = transform.localScale * scale;
        }
        
        if (pcConfig.pointClouds[ExperimentManager.index].width > 0f)
        {
            float length = Mathf.Max(bound.extents.x * 2f, bound.extents.z * 2f);
            float scale = (float)pcConfig.pointClouds[ExperimentManager.index].width / length;
            transform.localScale = transform.localScale * scale;
        }


        bound = GetComponent<MeshRenderer>().bounds;

        Vector3 horiOffset = new Vector3(-bound.center.x, 0f, -bound.center.z);
        transform.position += horiOffset;

        if (pcConfig.pointClouds[ExperimentManager.index].useStage > -1)
        {
            if(pcConfig.pointClouds[ExperimentManager.index].useStage >= 1)
            {
                if (bound.extents.y * 2f < maxHeight)
                {
                    float off = 0.5f+bound.extents.y-bound.center.y;
                    transform.position += Vector3.up * off;
                    GameObject go = Resources.Load<GameObject>("Stage");
                    stage = Instantiate(go);
                    stage.transform.localScale = new Vector3(bound.extents.x * (2f + 0.2f), 0.5f, bound.extents.z * (2f + 0.2f));
                    stage.transform.position = Vector3.up * 0.5f / 2.0f;
                    stage.transform.parent = transform;
                    if (enableStageShadow)
                    {
                        
                        shadowStageQuad = Instantiate(Resources.Load<GameObject>("ShadowStageQuad"));
                        MeshRenderer stageQuadRenderer = shadowStageQuad.GetComponent<MeshRenderer>();
                        Texture stageTex = Resources.Load<Texture>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name + "_shadowTexture");
                        
                        stageQuadRenderer.material.mainTexture = stageTex;

                        shadowStageQuad.transform.position = new Vector3(0, 0.5f + 0.001f, 0);
                        shadowStageQuad.transform.localScale = new Vector3(bound.extents.x * (2f + 0.2f), bound.extents.z * (2f + 0.2f), 1f);
                        shadowStageQuad.transform.parent = transform;
                    }
                } else
                {
                    float offset = bound.extents.y - bound.center.y;
                    transform.position += Vector3.up * offset;
                    shadowQuad = Instantiate(Resources.Load<GameObject>("ShadowQuad"));
                    MeshRenderer quadRenderer = shadowQuad.GetComponent<MeshRenderer>();
                    Texture tex = Resources.Load<Texture>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name + "_shadowTexture");
                    quadRenderer.material.mainTexture = tex;
                    shadowQuad.transform.localScale = new Vector3(bound.extents.x * 2f, bound.extents.z * 2f, 1f);
                    shadowQuad.transform.position = new Vector3(0, 0.0001f, 0);
                    shadowQuad.transform.parent = transform;
                }
            } else
            {
                float offset = bound.extents.y - bound.center.y;
                transform.position += Vector3.up * offset;
                shadowQuad = Instantiate(Resources.Load<GameObject>("ShadowQuad"));
                MeshRenderer quadRenderer = shadowQuad.GetComponent<MeshRenderer>();
                Texture tex = Resources.Load<Texture>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name + "_shadowTexture");
                quadRenderer.material.mainTexture = tex;
                shadowQuad.transform.localScale = new Vector3(bound.extents.x * 2f, bound.extents.z * 2f, 1f);
                shadowQuad.transform.position = new Vector3(0, 0.0001f, 0);
                shadowQuad.transform.parent = transform;
            }
        }
        else
        {
            if (bound.center.y < centerHeight)
            {
                float offset = centerHeight - transform.position.y;
                transform.position += Vector3.up * offset;
                GameObject go = Resources.Load<GameObject>("Stage");
                stage = Instantiate(go);
                stage.transform.localScale = new Vector3(bound.extents.x * (2f + 0.2f), offset, bound.extents.z * (2f + 0.2f));
                stage.transform.position = Vector3.up * offset / 2.0f;
                if (enableStageShadow)
                {
                    shadowStageQuad = Instantiate(Resources.Load<GameObject>("ShadowStageQuad"));
                    MeshRenderer stageQuadRenderer = shadowStageQuad.GetComponent<MeshRenderer>();
                    Texture stageTex = Resources.Load<Texture>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name + "_shadowTexture");
                    stageQuadRenderer.material.mainTexture = stageTex;
                    shadowStageQuad.transform.position = new Vector3(0, offset + 0.001f, 0);
                    shadowStageQuad.transform.localScale = new Vector3(bound.extents.x * (2f + 0.2f), bound.extents.z * (2f + 0.2f), 1f);
                    shadowStageQuad.transform.parent = transform;
                }

            }
            else
            {
                shadowQuad = Instantiate(Resources.Load<GameObject>("ShadowQuad"));
                MeshRenderer quadRenderer = shadowQuad.GetComponent<MeshRenderer>();
                Texture tex = Resources.Load<Texture>("Materials/" + GetComponent<MeshFilter>().sharedMesh.name + "_shadowTexture");
                quadRenderer.material.mainTexture = tex;
                shadowQuad.transform.localScale = new Vector3(quadScale.x, quadScale.y, 1f);
                shadowQuad.transform.position = new Vector3(quadPos.x, 0.0001f, quadPos.y);
                shadowQuad.transform.parent = transform;
            }
        }

        teleportLocked = GameObject.FindGameObjectsWithTag("TeleportLocked");
        foreach(GameObject teleportLockedObj in teleportLocked) {
            teleportLockedObj.transform.localScale = new Vector3(bound.extents.x * (2f + 0.2f), 1f, bound.extents.z * (2f + 0.2f));
        }
    }

    public void MoveObject()
    {
        if (isDistorted)
        {
            transform.position += Vector3.right * 2.5f;
        }
        else
        {
            transform.position -= Vector3.right * 2.5f;
        }
    }

    private void OnDisable()
    {
        if (stage != null)
        {
            Destroy(stage);
        }
        if (shadowQuad != null)
        {
            Destroy(shadowQuad);
        }
        if(shadowStageQuad != null)
        {
            Destroy(shadowStageQuad);
        }
    }

}
