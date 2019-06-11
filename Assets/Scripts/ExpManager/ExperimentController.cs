using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ExperimentController : MonoBehaviour {

    private SteamVR_Behaviour_Pose bp;
    private SteamVR_Input_Sources inputSource;
    public GameObject choices;
    private static bool isChoosing;
    public bool currentSelectingHand;
    private ExperimentController otherController;
    public LayerMask menuLayer;
    private LineRenderer lr;
    private static Collider previousHitCol;
    private static Collider hitCol;
    // Use this for initialization
    void Start () {
        isChoosing = false;
        bp = GetComponent<SteamVR_Behaviour_Pose>();
        inputSource = bp.inputSource;
        otherController = GetComponent<Hand>().otherHand.GetComponent<ExperimentController>();
        lr = GetComponent<LineRenderer>();
        previousHitCol = null;
        hitCol = null;
    }

    // Update is called once per frame
    void Update()
    {

        if (!currentSelectingHand)
        {
            lr.enabled = false;
        }

        if (isChoosing && currentSelectingHand)
        {
            lr.enabled = true;
            CheckRaycast();
        }

        if (SteamVR_Input._default.inActions.GrabGrip.GetStateDown(inputSource))
        {
            SwitchMenu();
        }
        if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(inputSource))
        {
            if (!isChoosing)
            {
                if (ExperimentManager.currentMode == ExperimentManager.ExperimentMode.deux)
                {
                    ExperimentManager.isDistorted = !ExperimentManager.isDistorted;
                }
            }
            else
            {
                if(currentSelectingHand)
                {
                    if (hitCol != null)
                    {
                        if (hitCol.GetComponent<HitResponse>())
                        {
                            HitResponse hr = hitCol.GetComponent<HitResponse>();
                            hr.OnHitPressedDown();
                        }
                    }
                }
            }

        }
        if (SteamVR_Input._default.inActions.GrabPinch.GetState(inputSource))
        {
            if (isChoosing && currentSelectingHand)
            {
                if (hitCol != null)
                {
                    if (hitCol.GetComponent<HitResponse>())
                    {
                        HitResponse hr = hitCol.GetComponent<HitResponse>();
                        hr.OnHitPressed();
                    }
                }
            }
        }

        if (SteamVR_Input._default.inActions.GrabPinch.GetStateUp(inputSource))
        {

            if (isChoosing)
            {
                if (!currentSelectingHand)
                {
                    currentSelectingHand = true;
                    otherController.currentSelectingHand = false;
                    lr.enabled = true;
                }
                else
                {
                    if (hitCol != null)
                    {
                        if (hitCol.GetComponent<HitResponse>())
                        {
                            HitResponse hr = hitCol.GetComponent<HitResponse>();
                            hr.OnHitPressedUp();
                            SwitchMenu();
                        }
                    }
                }
            }
        }
    }

    private void SwitchMenu()
    {
        isChoosing = !isChoosing;
        choices.SetActive(isChoosing);
        lr.enabled = isChoosing;
        if (!isChoosing)
        {
            otherController.GetComponent<LineRenderer>().enabled = isChoosing;
        }
    }

    private void CheckRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 5f, menuLayer))
        {
            lr.SetPosition(1, transform.InverseTransformPoint(hit.point));
            hitCol = hit.collider;
        } else
        {
            hitCol = null;
        }
	
        if(previousHitCol != hitCol)
        {
            if (hitCol!=null && hitCol.GetComponent<HitResponse>())
            {
                HitResponse hr = hitCol.GetComponent<HitResponse>();
                hr.OnHitEnter();
            }
            if (previousHitCol != null && previousHitCol.GetComponent<HitResponse>())
            {
                HitResponse hr = previousHitCol.GetComponent<HitResponse>();
                hr.OnHitEnd();
            }
        } else
        {
            if (hitCol != null && hitCol.GetComponent<HitResponse>())
            {
                HitResponse hr = hitCol.GetComponent<HitResponse>();
                hr.OnHitStay();
            }
        }
	
        previousHitCol = hitCol;
    }
}
