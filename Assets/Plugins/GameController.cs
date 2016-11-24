using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    [SerializeField]
    public GameObject UIController;
    public GameObject UIScene;
    public GameObject UIMain;


    public GameObject UIBegin;
    public GameObject UIStop;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void EnterSecne()
    {
        UIScene.SetActive(true);
        UIMain.SetActive(false);
        UIController.SetActive(false);
    }

    public void EnterMain()
    {
        UIScene.SetActive(false);
        UIMain.SetActive(true);
        UIController.SetActive(false);
    }

    public void EnterController()
    {
        UIScene.SetActive(false);
        UIMain.SetActive(false);
        UIController.SetActive(true);
    }

    public void SwitchUI()
    {
        UIBegin.SetActive(false);
        UIStop.SetActive(true);
    }
}
