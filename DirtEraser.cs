using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirtEraser : MonoBehaviour
{
    [SerializeField] OVRInput.Controller controller = OVRInput.Controller.Hands;
    [SerializeField] float Kp=1;
    [SerializeField] Transform anchor;
    [SerializeField] GameObject gameManagerObject;
    private GameManager gameManager;
    private float maxDist = 0.50f;
    private float sqrMaxDist;
    private float tableVibTime = 0.2f;
    private float dirtVibTime = 0.1f;
    private bool newVibrationStarting = false;
    

    // Start is called before the first frame update
    void Start()
    {
        gameManager = gameManagerObject.GetComponent<GameManager>();
        sqrMaxDist = maxDist * maxDist;
    }

    // Update is called once per frame
    void Update()
    {
        MoveToAnchor();
    }

    private void MoveToAnchor()
    {
        if((anchor.position - transform.position).magnitude > sqrMaxDist)
        {
            transform.position = anchor.position;
        }
        else
        {
            Vector3 error = anchor.position - transform.position;
            Vector3 controlSignal = Kp * error;
            GetComponent<Rigidbody>().AddForce(controlSignal, ForceMode.Acceleration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Dirt"))
        {
            //if you collide with dirt, then erase the dirt
            Destroy(other.transform.parent.gameObject);
            StartCoroutine(Vibrate(dirtVibTime));
            gameManager.RecordErasedDirt();
        }
        else if (other.gameObject.CompareTag("SpawnButton"))
        {
            //if you collide with the button, spawn dirt.
            StartCoroutine(Vibrate(tableVibTime));
            gameManager.SetHeight(other.gameObject);
            gameManager.SpawnDirt();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            StartCoroutine(Vibrate(tableVibTime));
        }
    }

    private IEnumerator Vibrate(float t)
    {
        Debug.Log("Entering vibration coroutine");
        //OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        //OVRInput.SetControllerVibration(1, 1, controller);
        ///*if(controller != OVRInput.Controller.Hands)
        //{
            Debug.Log("Vibrating controller");
            bool latestVibration = true;
            newVibrationStarting = true;
            OVRInput.SetControllerVibration(0.5f, 0.5f, controller);
            yield return new WaitForSeconds(2.0f * t / 25.0f);
            newVibrationStarting = false;
            for (float i = 0; i < (25-2); i++)
            {
                yield return new WaitForSeconds(1.0f * t / 25.0f);
                if (newVibrationStarting) latestVibration = false;
            }
            if(latestVibration)
                OVRInput.SetControllerVibration(0, 0, controller);
        //}*/

        yield return null;
    }
}
