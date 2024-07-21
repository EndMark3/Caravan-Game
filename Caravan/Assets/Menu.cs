using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject ReallyQuitButton;

    float reallyQuitTimer;

    void Start()
    {
        ToggleMenu(false);
    }

    void Update()
    {
        if(reallyQuitTimer > 0)
        {
            reallyQuitTimer -= Time.deltaTime;

            if(reallyQuitTimer <= 0)
            {
                ReallyQuitButton.SetActive(false);
            }
        }

        if(Input.GetButtonDown("Cancel"))
        {
            Debug.Log("Pause menu");
            ToggleMenu(!PauseMenu.activeSelf);
        }
    }

    public void ToggleMenu(bool active)
    {
        PauseMenu.SetActive(active);

        Cursor.visible = active;
        if (active)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Quit()
    {
        reallyQuitTimer = 5f;
        ReallyQuitButton.SetActive(true);
    }

    public void ReallyQuit()
    {
        Application.Quit();
    }
}
