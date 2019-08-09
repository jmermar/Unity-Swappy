﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    private bool paused = false;

    public bool Paused {
        get {
            return paused;
        }
    }

    private bool won = false;
    private bool dead = false;

    private float counter = 0.0f, deadCount = 0.5f;

    public Canvas winMenu;
    // Start is called before the first frame update
    void Start()
    {
        Restart();
    }

    void Restart()
    {
        won = false;
        dead = false;
        paused = false;

        winMenu.gameObject.SetActive(false);

        PlayerSpawn p = (PlayerSpawn) FindObjectOfType(typeof(PlayerSpawn));

        p.SpawnPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        if (won)
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                GlobalData.Level = (GlobalData.Level + 1) % GlobalData.NumLevels;

                SceneManager.LoadScene("Scenes/Level " + GlobalData.Level);
            }
        }

        if (dead)
        {
            if (counter > 0)
            {
                counter -= Time.deltaTime;
            } else
            {
                Object.Destroy(FindObjectOfType<Player>().gameObject);
                Restart();
            }
        }
    }

    public void CompleteLevel()
    {
        paused = true;

        won = true;

        winMenu.gameObject.SetActive(true);
    }

    public void KillPlayer()
    {
        paused = true;

        dead = true;

        counter = deadCount;
    }
}
