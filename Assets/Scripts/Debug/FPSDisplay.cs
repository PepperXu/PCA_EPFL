using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour {

    private Text fpsText;
    private float fps;
    private int frameCount;
    private float startTime;
	// Use this for initialization
	void Start () {
        fps = 0f;
        frameCount = 0;
        fpsText = GetComponent<Text>();
        fpsText.text = fps.ToString();
	}

    // Update is called once per frame
    void Update() {
        if (frameCount == 0)
        {
            startTime = Time.time;
        }
        if (frameCount < 20)
        {
            frameCount++;
        }
        else
        {
            frameCount = 0;
            float deltaTime = Time.time - startTime;
            fps = 20 / deltaTime;
            fpsText.text = fps.ToString();
        }
	}
}
