using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] GameObject dirtOrigin;
    [SerializeField] GameObject dirtPrefab;
    [SerializeField] GameObject tableTopTransform;

    [SerializeField] GameObject spawnButton1;
    [SerializeField] GameObject spawnButton2;
    [SerializeField] float height1 = 0.98f;
    [SerializeField] float height2 = 1.25f;

    private bool gameActive;
    private int numDirt;

    // Start is called before the first frame update
    void Start()
    {
        //SpawnDirt();
    }

    // Update is called once per frame
    void Update()
    {
        if(gameActive && numDirt == 0)
        {
            spawnButton1.SetActive(true);
            spawnButton2.SetActive(true);
            gameActive = false;
        }
    }

    private void EraseRemainingDirt()
    {
        foreach (Transform child in dirtOrigin.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        numDirt = 0;
    }

    public void SetHeight(GameObject button)
    {
        if (button.GetInstanceID() == spawnButton1.GetInstanceID())
            tableTopTransform.transform.localPosition = new Vector3(0, height1);
        if (button.GetInstanceID() == spawnButton2.GetInstanceID())
            tableTopTransform.transform.localPosition = new Vector3(0, height2);
    }

    public void SpawnDirt()
    {
        numDirt = 0;
        for(int x = 0; x < 10; x++)
            for(int z = 0; z < 10; z++)
            {
                GameObject newDirt = Instantiate(dirtPrefab, dirtOrigin.transform);
                newDirt.transform.localPosition = new Vector3(x, 0, z);
                numDirt++;
            }
        gameActive = true;
        spawnButton1.SetActive(false);
        spawnButton2.SetActive(false);
    }

    public void RecordErasedDirt()
    {
        numDirt--;
    }
}
