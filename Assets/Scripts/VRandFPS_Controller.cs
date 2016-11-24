using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VRandFPS_Controller : MonoBehaviour {

    [SerializeField]
    public GameObject UIFPS;

    private OpenDiveSensor openDiveSensor;

    void Start()
    {
        openDiveSensor = this.gameObject.GetComponent<OpenDiveSensor>();
    }

    public void ToUIvr()
    {
        openDiveSensor.enabled = true;
        UIFPS.SetActive(false);
    }

    public void ToUIFPS()
    {
        openDiveSensor.enabled = false;
        UIFPS.SetActive(true);
    }
}
