using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace extOSC 
{
    public class ConductorManager : MonoBehaviour
    {
        #region Public Vars

        public GameObject baton;
        public GameObject headset;
        public GameObject offhand;
        public GameObject oboeObj;
        public GameObject clarObj;
        [SerializeField] float smoothnessLow=0.1f;
        [SerializeField] float smoothnessHigh=1.0f;
        [SerializeField] int barlineThreshold = 15;
        [SerializeField] float downstrokeRatio = 0.1f;
        [SerializeField] float dynamicLow = 0.1f;
        [SerializeField] float dynamicHigh = 1.0f;
        
        //The following variables are exposed for debugging purposes, but do not need to be modified on the editor side

        [Header("Key Variables")]
        [SerializeField] private float tempo = 108f;        //bpm
        [SerializeField] private float smoothness = 0.9f;     //range [0..1] where 0 is sharp and 1 is smooth
        [SerializeField] private float amplitude = 0.5f;         //range [0..1] where 0 is quiet and 1 is loud

        [Header("Speed Variables")]
        [SerializeField] private float speedStdDev = 0;
        [SerializeField] private float speedMean = 0;
        [SerializeField] private float currSpeed = 0;

        [Header("Volume Variables")]
        [SerializeField] private float yrange;
        [SerializeField] private float xzrange;
        [SerializeField] private float oboe1mod = 0f;
        [SerializeField] private float clar2mod = 0f;

        [Header("OSC Settings")]
        public OSCTransmitter Transmitter;

        #endregion

      
        
        #region Private Vars
           
        private List<Vector3> batonPositions = new List<Vector3>();
        private Vector3[] prevPos = new Vector3[5];
        private List<float> batonSpeeds = new List<float>();

        private int downstrokeCount = 0;

        private float prevTime = -99f;

        private float angSpeedMag = 0f;
        private Vector3 angSpeedAxis = Vector3.zero;
        private Quaternion prevRotation = Quaternion.identity;

        
        //for simplicity, I saved the frequency of each tone as a constant
        private float F3 = 194.9977f;
        private float G3 = 195.9977f;
        private float A3 = 220f;
        private float Bb3 = 223.0819f;
        private float C4 = 261.6256f;
        private float D4 = 293.6648f;
        private float Eb4 = 311.127f;
        private float F4 = 349.2282f;
        private float G4 = 391.9954f;
        private float A4 = 440f;
        private float Bb4 = 466.1638f;
        private float C5 = 523.2511f;
        private float D5 = 587.3295f;
        private float Eb5 = 622.254f;
        private float F5 = 698.4565f;
        private float G5 = 783.9909f;
        private float A5 = 880f;
        private float Bb5 = 932.3275f;
        private float C6 = 1046.502f;

        private string[] bars = { "playBar1", "playBar2", "playBar3", "playBar4" };
        private int numBars = 4;
        private int nextBar = 0;

        #endregion




        #region Unity Methods


        private void FixedUpdate()
        {
            //check whether the use is gesturing to a specific section
            ParseLeftHand();
        }

        void Update()
        {
            //gradually bring manually adjusted volumes back to the orchestra's level
            oboe1mod -= Mathf.Sign(oboe1mod) * 0.002f;
            clar2mod -= Mathf.Sign(clar2mod) * 0.002f;

            //record the baton position
            TrackBaton();
            //if the user is completing a downstroke, play the next bar.
            if (CheckForBarline())
                ParseBar();
        }

        private void ParseLeftHand()
        {
            //check if the use is looking at one of the sections
            RaycastHit hit;
            if (Physics.Raycast(headset.transform.position, headset.transform.forward, out hit, Mathf.Infinity))
            {
                Debug.DrawRay(headset.transform.position, headset.transform.forward * hit.distance, Color.yellow);

                //check if the palm normal is aligned with the vertical axis
                Vector3 offHandDirection = offhand.transform.right;
                if (Mathf.Abs(offHandDirection.y / offHandDirection.x) > 0.9f &&
                    Mathf.Abs(offHandDirection.y / offHandDirection.z) > 0.9f)
                {
                    Debug.Log(hit.collider.tag);
                    Debug.DrawRay(offhand.transform.position, offHandDirection);
                    //increase or decrease a section's volume depending on whether the hand is up or down
                    if (hit.collider.tag == "oboes")
                        oboe1mod += 0.010f * Mathf.Sign(offHandDirection.y);
                    else if (hit.collider.tag == "clarinets")
                        clar2mod += 0.010f * Mathf.Sign(offHandDirection.y);
                }
            }
        }

        private void TrackBaton()
        {
            //This function records the baton position and velocity and updates the smoothness

            //record position
            batonPositions.Add(baton.transform.position);

            //record velocity
            Vector3 batonVelocity = GetBatonVelocity();
            currSpeed = batonVelocity.magnitude;
            batonSpeeds.Add(GetAngularSpeed());

            //update downstroke count
            downstrokeCount = (Mathf.Abs(batonVelocity.x / batonVelocity.y) < downstrokeRatio &&
                                Mathf.Abs(batonVelocity.z / batonVelocity.y) < downstrokeRatio &&
                                batonVelocity.y < 0 ?
                                downstrokeCount + 1 : 
                                0);
        }

        private Vector3 GetBatonVelocity()
        {
            //this fuction looks at the 5 most recent baton positions and uses a finite difference to approximate the derivative
            prevPos[4] = prevPos[3];
            prevPos[3] = prevPos[2];
            prevPos[2] = prevPos[1];
            prevPos[1] = prevPos[0];
            prevPos[0] = baton.transform.position;

            Vector3 velocity;
            velocity = (3 * prevPos[4] - 16 * prevPos[3] + 36 * prevPos[2] - 48 * prevPos[1] + 25 * prevPos[0]) / (4 * Time.deltaTime);
            return velocity;
        }

        private float GetAngularSpeed()
        {
            //https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/
            Quaternion deltaRotation = baton.transform.rotation * Quaternion.Inverse(prevRotation);
            deltaRotation.ToAngleAxis(out angSpeedMag, out angSpeedAxis);
            prevRotation = baton.transform.rotation;
            return angSpeedMag;
        }

        private bool CheckForBarline()
        {
            //check for a downstroke
            if(downstrokeCount > barlineThreshold)
            {
                downstrokeCount = 0;
                return true;
            }
            return false;
            //if it's not working, try adjusting the barlineThreshold with tempo. 
        }

        private void ParseBar()
        {
            //This function updates tempo, updates dynamic, and sets the next bar to play

            

            //update tempo
            float currTime = Time.time;
            if (currTime - prevTime > 0.7f * 60f / tempo * 4f)    //if not too short
            {
                if (currTime - prevTime < 1.4f * 60f / tempo * 4f) //if not too long
                {
                    tempo = 60f / (currTime - prevTime) * 4f;
                    //update smoothness
                    CalcSmoothness();
                    //update dynamic
                    CalcDynamic();
                }
                //even if it is too long...
                //set next bar to play
                playNextBar();
                batonPositions.Clear();
                batonSpeeds.Clear();
                prevTime = currTime;
            }

        }

        private void CalcSmoothness()
        {
            //calculate the mean
            speedMean = 0f;
            foreach (float s in batonSpeeds)
                speedMean += s;
            speedMean /= batonSpeeds.Count;

            foreach (float s in batonSpeeds)
                speedStdDev += Mathf.Pow(s - speedMean, 2f);
            speedStdDev = Mathf.Sqrt(speedStdDev / batonSpeeds.Count);

            //update the smoothness
            smoothness = (speedStdDev - smoothnessLow) / (smoothnessHigh - smoothnessLow);
            if (smoothness < 0.1f) smoothness = 0.1f;
            if (smoothness > 0.99f) smoothness = 0.99f;
        }

        private void CalcDynamic()
        {
            float minx, maxx, miny, maxy, minz, maxz;
            minx = miny = minz = 99f;
            maxx = maxy = maxz = -99f;

            foreach(Vector3 pos in batonPositions)
            {
                Debug.Log(pos); 
                if (pos.x < minx) minx = pos.x;
                if (pos.x > maxx) maxx = pos.x;
                if (pos.y < miny) miny = pos.y;
                if (pos.y > maxy) maxy = pos.y;
                if (pos.z < minz) minz = pos.z;
                if (pos.z > maxz) maxz = pos.z;
            }
            Debug.Log(minx.ToString() + " " +
                maxx.ToString() + " " +
                miny.ToString() + " " +
                maxy.ToString() + " " +
                minz.ToString() + " " +
                maxz.ToString());
            yrange = maxy - miny;
            xzrange = Mathf.Sqrt((maxx - minx) * (maxx - minx) + (maxz - minz) * (maxz - minz));
            float avgRange = (yrange + xzrange) / 2;

            //avgRange is a metric for how large the motion is.
            //if avgRange is zero, the motion is tiny.  If large, the motion is grand.
            amplitude = (avgRange - dynamicLow) / (dynamicHigh - dynamicLow);
            if (amplitude < 0f) amplitude = 0f;
            if (amplitude > 1f) amplitude = 1f;
        }

        private void SendMsg(string address, float freq, float amp, float dur)
        {
            //Send a message through the OSC Manager. 
            var message = new OSCMessage("/notePlayer");
            message.AddValue(OSCValue.String(freq.ToString() + " " + amp.ToString() + " " + dur.ToString() + " " + address));

            Transmitter.Send(message);
        }

        private void playNextBar()
        {
            //loop through starting a thread to play each bar
            StartCoroutine(bars[nextBar]);
            nextBar = (nextBar + 1 ) % numBars;
        }

        private IEnumerator PulseObject(GameObject obj)
        {
            //When a section plays a note, this function starts a new thread which grows and then slowly shrinks the object
            Vector3 ones = Vector3.one;
            obj.transform.localScale += 0.1f * ones;
            for(int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(0.01f);
                obj.transform.localScale -= 0.005f * ones;
            }
        }



        //below are the hard-coded first four bars of the piece. 
        //Each coroutine will send messages to Supercollider to play notes, wait a beat or half-beat, and send the next messages, etc.
        private IEnumerator playBar1()
        {
            float beatLength = 60.0f / tempo; //1/(bpm/(s/min)) = 60/bpm
            float eighth = beatLength / 2 * smoothness;
            float quarter = beatLength * (smoothness + (1 - smoothness) / 2);
            SendMsg("\\oboe1", F5, amplitude+oboe1mod, eighth);     StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", Bb3, amplitude+clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\oboe1", F5, amplitude+oboe1mod, eighth);     StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\clar2", C4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\oboe1", F5, amplitude+oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\oboe1", F5, amplitude+oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", D4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\oboe1", F5, amplitude+oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\clar2", Bb3, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);        
            SendMsg("\\oboe1", Bb5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));

        }

        private IEnumerator playBar2()
        {
            float beatLength = 60.0f / tempo; //1/(bpm/(s/min)) = 60/bpm
            float eighth = beatLength / 2 * smoothness;
            float quarter = beatLength * (smoothness + (1 - smoothness) / 2);
            float gracenote = beatLength * 0.05f;
            SendMsg("\\oboe1", Bb5, amplitude + oboe1mod, gracenote);
            SendMsg("\\oboe1", A5, amplitude + oboe1mod, quarter); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", F4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength);

            SendMsg("\\clar2", Eb4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);

            SendMsg("\\oboe1", G5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength / 2);

            SendMsg("\\clar2", D4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            SendMsg("\\oboe1", G5, amplitude + oboe1mod, gracenote);
            SendMsg("\\oboe1", F5, amplitude + oboe1mod, quarter); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength);


            SendMsg("\\clar2", C4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);

            SendMsg("\\oboe1", Eb5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
        }

        private IEnumerator playBar3()
        {
            float beatLength = 60.0f / tempo; //1/(bpm/(s/min)) = 60/bpm
            float eighth = beatLength / 2 * smoothness;
            float quarter = beatLength * (smoothness + (1 - smoothness) / 2);
            SendMsg("\\oboe1", D5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", Bb3, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength);
            SendMsg("\\oboe1", Bb4, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", D4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength);
            SendMsg("\\oboe1", Eb5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", C4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength);
            SendMsg("\\oboe1", D5, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", Bb3, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));

        }

        private IEnumerator playBar4()
        {
            float beatLength = 60.0f / tempo; //1/(bpm/(s/min)) = 60/bpm
            float eighth = beatLength / 2;
            float quarter = beatLength * (smoothness + (1 - smoothness) / 2);
            float sixteenth = beatLength / 4;
            float gracenote = beatLength * 0.05f;

            SendMsg("\\oboe1", D5, amplitude + oboe1mod, gracenote);
            SendMsg("\\oboe1", C5, amplitude + oboe1mod, quarter); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", F4, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength);
            
            
            SendMsg("\\oboe1", Bb4, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", F3, amplitude + clar2mod, quarter); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 2);
            SendMsg("\\oboe1", A4, amplitude + oboe1mod, eighth); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength / 2);
            
            
            SendMsg("\\oboe1", G4, amplitude + oboe1mod, gracenote);
            SendMsg("\\oboe1", F4, amplitude + oboe1mod, quarter); StartCoroutine(PulseObject(oboeObj));
            yield return new WaitForSeconds(beatLength);



            SendMsg("\\oboe1", Bb4, amplitude + oboe1mod, sixteenth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", Bb3, amplitude + clar2mod, sixteenth); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 4);
            SendMsg("\\oboe1", C5, amplitude + oboe1mod, sixteenth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", C4, amplitude + clar2mod, sixteenth); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 4);
            SendMsg("\\oboe1", D5, amplitude + oboe1mod, sixteenth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", D4, amplitude + clar2mod, sixteenth); StartCoroutine(PulseObject(clarObj));
            yield return new WaitForSeconds(beatLength / 4);
            SendMsg("\\oboe1", Eb5, amplitude + oboe1mod, sixteenth); StartCoroutine(PulseObject(oboeObj));
            SendMsg("\\clar2", Eb4, amplitude + clar2mod, sixteenth); StartCoroutine(PulseObject(clarObj));
        }

        #endregion
    }
}