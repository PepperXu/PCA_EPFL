using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateChoices : MonoBehaviour {
    public GameObject choicePrefab;
    public static string[] choices;
	// Use this for initialization
	void Start () {
        if (choices != null)
        {
            float interval = 0.5f / choices.Length;
            for(int i = 0; i < choices.Length; i++)
            {
                GameObject choice = Instantiate(choicePrefab) as GameObject;
                Vector3 pos = choice.transform.position;
                Vector3 rot = choice.transform.eulerAngles;
                Vector3 scale = choice.transform.localScale;
                choice.transform.parent = transform;
                choice.transform.localPosition = pos + Vector3.up*(0.25f - i * interval);
                choice.transform.localEulerAngles = rot;
                choice.transform.localScale = scale;
                choice.GetComponent<HitResponse>().level = i;
                choice.GetComponentInChildren<Text>().text = choices[i];

            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
