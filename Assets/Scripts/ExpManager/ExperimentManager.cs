using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;

public class ExperimentManager : MonoBehaviour {


    public string configFile;
    private string dataPath = "/StreamingAssets/";
    private PointCloudConfig pcConfig;
    private GameObject currentObject;
    private GameObject currentDistortedObjectForParaMode;
    public static int index;
    private int preindex;
    public static bool isDistorted;
    private bool preisdistorted;
    private int pointCloudCount;

    public GameObject env1, env2;

    public static ExperimentMode currentMode;

    public enum ExperimentMode
    {
        single, deux, para
    }

    [System.Serializable]
    public class PointCloud
    {
        public string original;
        public string distorted;
    }

    [System.Serializable]
    public class PointCloudConfig
    {
        public PointCloud[] pointClouds;
        public string expMode = "para";
        public bool useInterpolation = true;
        public float pointSize = -1;
        public byte[] tint = { 255, 255, 255 };
        public string[] ratingScales;
    }

    // Use this for initialization
    void Awake() {

        dataPath = dataPath + configFile + ".json";
        string filePath = Application.dataPath + this.dataPath;

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            pcConfig = JsonUtility.FromJson<PointCloudConfig>(dataAsJson);
            if (pcConfig.expMode.StartsWith("double"))
            {
                currentMode = ExperimentMode.deux;
            }
            else if(pcConfig.expMode.StartsWith("para"))
            {
                currentMode = ExperimentMode.para;
            } else if (pcConfig.expMode.StartsWith("single")){
                currentMode = ExperimentMode.single;
            } else
            {
                currentMode = ExperimentMode.para;
            }
            
            if(currentMode == ExperimentMode.para)
            {
                env2.SetActive(true);
                env1.SetActive(false);
            } else
            {
                env1.SetActive(true);
                env2.SetActive(false);
            }
            MatConfig.useInterpolation = pcConfig.useInterpolation;
            MatConfig.pointSize = pcConfig.pointSize;
            MatConfig.tint = pcConfig.tint;
            currentObject = null;
            index = 0;
            preindex = index;
            isDistorted = false;
            preisdistorted = isDistorted;
            pointCloudCount = pcConfig.pointClouds.Length;
            if(pcConfig.ratingScales != null)
            {
                UpdateChoices.choices = pcConfig.ratingScales;
            } else
            {
                Debug.LogError("Choices not specified!");
            }
        }
        else
        {
            Debug.LogError("Config not found!");
            Application.Quit();
        }
        CheckIndexUpdate();
        Application.targetFrameRate = -1;
    }


    // Update is called once per frame
    void Update() {
        CheckIndexUpdate();
	}
        
    void CheckIndexUpdate()
    {
        if (currentObject == null || index != preindex || preisdistorted != isDistorted)
        {
            if (index >= pointCloudCount)
            {
                index = 0;
            }

            if (currentMode == ExperimentMode.para)
            {
                GameObject currentOriginal = Resources.Load<GameObject>(pcConfig.pointClouds[index].original);
                GameObject currentDistorted = Resources.Load<GameObject>(pcConfig.pointClouds[index].distorted);
                if (currentOriginal != null && currentDistorted != null)
                {
                    if (currentObject != null)
                    {
                        Destroy(currentObject);
                        Destroy(currentDistortedObjectForParaMode);
                    }
                    currentObject = Instantiate(currentOriginal) as GameObject;
                    if (currentObject.GetComponent<Stage>())
                    {
                        currentObject.GetComponent<Stage>().isDistorted = false;
                        currentObject.GetComponent<Stage>().MoveObject();
                    }
                    currentDistortedObjectForParaMode = Instantiate(currentDistorted) as GameObject;
                    if (currentDistortedObjectForParaMode.GetComponent<Stage>())
                    {
                        currentDistortedObjectForParaMode.GetComponent<Stage>().isDistorted = true;
                        currentDistortedObjectForParaMode.GetComponent<Stage>().MoveObject();
                    }
                } else
                {
                    Debug.Log("OBJ NOT FOUND");
                }
            }
            else
            {

                GameObject currentObjectRef = Resources.Load<GameObject>(isDistorted ? pcConfig.pointClouds[index].distorted : pcConfig.pointClouds[index].original);

                if (currentObjectRef != null)
                {
                    if (currentObject != null)
                    {
                        Destroy(currentObject);
                    }
                    currentObject = Instantiate(currentObjectRef) as GameObject;
                }
                else
                {
                    Debug.Log("OBJ NOT FOUND");
                }
            }
        }
        preindex = index;
        preisdistorted = isDistorted;
    }

    public static void RecordResult(int level)
    {

    }

}
