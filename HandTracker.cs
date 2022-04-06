using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTracker : MonoBehaviour
{

    [SerializeField] GameObject leftControllerBrush;
    [SerializeField] GameObject leftHandCollider;
    [SerializeField] GameObject rightControllerBrush;
    [SerializeField] GameObject rightHandCollider;
    [SerializeField] Transform leftAnchor;
    [SerializeField] Transform rightAnchor;
    [SerializeField] Transform gameTransform;
    [SerializeField] float setFloorThreshold = 0.5f;

    private bool settingFloor = false;

    // Start is called before the first frame update
    private OVRHand[] m_hands;
    private void Awake()
    {
        m_hands = new OVRHand[]
        {
            GameObject.Find("OVRCameraRig/TrackingSpace/LeftHandAnchor/OVRHandPrefab").GetComponent<OVRHand>(),
            GameObject.Find("OVRCameraRig/TrackingSpace/RightHandAnchor/OVRHandPrefab").GetComponent<OVRHand>()
        };
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        leftControllerBrush.SetActive(!m_hands[0].IsTracked);
        //leftHandCollider.SetActive(m_hands[0].IsTracked);
        m_hands[0].GetComponent<SphereCollider>().enabled = m_hands[0].IsTracked;

        rightControllerBrush.SetActive(!m_hands[1].IsTracked);
        //rightHandCollider.SetActive(m_hands[1].IsTracked);
        m_hands[1].GetComponent<SphereCollider>().enabled = m_hands[1].IsTracked;

        float minHandHeight = Mathf.Min(leftAnchor.position.y,
                                        rightAnchor.position.y);

        if (minHandHeight < setFloorThreshold)
        {

            if (!settingFloor || minHandHeight < gameTransform.position.y)
            {
                gameTransform.position = Vector3.up * minHandHeight;
                settingFloor = true;
            }
        }
        else settingFloor = false;
    }
}
