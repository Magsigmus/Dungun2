using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RestartScript : MonoBehaviour
{
    [SerializeField]
    private Button button;
    private PlayerControls playerControls;

    private void Awake()
    {
        playerControls = new PlayerControls();
    }


    private void OnEnable() { playerControls.Enable(); }
    private void OnDisable() { playerControls.Disable(); }

    void Start()
    {
        //rasj: on button click, load scene
        button.onClick.AddListener(() => 
        {
            button.gameObject.SetActive(false);
            LoadScene();
        });
    }

    public void Update()
    {
        //Debug.Log(playerControls.Menu.Enter.phase);
        if(playerControls.Menu.Enter.phase == InputActionPhase.Performed)
        {
            LoadScene();
        }
    }

    public void LoadScene()
    {
        SceneManager.LoadScene("MainScene");  //rasj: 0 is the Level Generation 1 scene in build settings
    }
}
