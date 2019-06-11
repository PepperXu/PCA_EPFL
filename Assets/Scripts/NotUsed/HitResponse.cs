using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitResponse : MonoBehaviour {

    public Color normalColor;
    public Color highlightColor;
    public Color pressedColor;
    private Image img;
    public int level;

	// Use this for initialization
	void Start () {
        img = GetComponent<Image>();
	}

    public void OnHitEnter()
    {
        img.color = highlightColor;
    }

    public void OnHitEnd()
    {
        img.color = normalColor;
    }

    public void OnHitStay()
    {
        img.color = highlightColor;
    }

    public void OnHitPressedDown()
    {
        img.color = pressedColor;
    }

    public void OnHitPressed()
    {
        img.color = pressedColor;
    }

    public void OnHitPressedUp()
    {
        img.color = normalColor;
        ExperimentManager.index++;
        ExperimentManager.isDistorted = false;
        ExperimentManager.RecordResult(level);
    }
}
