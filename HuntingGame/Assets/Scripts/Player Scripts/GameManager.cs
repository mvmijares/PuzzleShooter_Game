﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/* What is our game?
 * - It is our version of Duck Hunt
 * - Users can move around, aim, and shoot at targets/
 * - Objective of the game
 *      - Given specific targets to find
 * -    - Must kill targets to aquire points.
 * - What makes the game difficult?
 *      - Limited ammo.
 *      - Animals move fast over the time.
 * - Win Condition?
 *      - Users have a total amount of targets to kill.
 *      - When the round is done, users get a grading (Kills / Total)
 *      - If they get a high enough grade, they proceed to next round.
 * - Gameplay Experience
 *      - Rounds should be 1 min each
 *      - Targets should "disappear" shortly after being spawned
 */
public class GameManager : MonoBehaviour
{
    [SerializeField] Player _player;
    public Player player { get { return _player; } }
    [SerializeField] EnemyManager _eManager;
    public EnemyManager eManager { get { return _eManager; } }
    [SerializeField] HUDManager _hudManager;
    public HUDManager hudManager { get { return _hudManager; } }
    [SerializeField] AudioHandler _audioHandler;
    public AudioHandler audioHandler { get { return _audioHandler; } }

    private int _roundNum;
    public int roundNum { get { return _roundNum; } }

    private bool pass; // if user passes to next round.

    [SerializeField] private float startTimer = 0.0f;
    public float startTime;

    [SerializeField] private float currRoundTime = 0.0f;
    public float GetRoundTime() {
        return roundTime - currRoundTime;
    } // returns round time
    public float roundTime = 60.0f;

    [Tooltip("Clip size for your weapon.")]
    public int weaponClipSize;

    private bool checkRoundStatus;
    private bool start = false; //main start condition.

    private bool isSpawnRunning; // spawn coroutine condition
    private EnemyType enemyType;
    private float score; // score is calculated at end of each round
    private float requiredScore; //required score to pass the round.
    private int killedEnemies; //Enemies that were killed
    public int numOfEnemies; //Enemies that you need to kill
    public int enemyHealthSize;

    public Transform playerSpawnPoint;
    [Tooltip("Radius of where player can move")]
    public float moveRadius;
    [SerializeField] private bool withinZone;
    private bool emptyClip;

    public int spawnPerSecond;
    private void Awake()
    {
        emptyClip = false;
        numOfEnemies = 10; // Prototype variable
        isSpawnRunning = false;

        _audioHandler = FindObjectOfType<AudioHandler>();
        if (_audioHandler)
            _audioHandler.ObjectInitialize(this);

        _player = FindObjectOfType<Player>();
        if (_player)
            _player.ObjectInitialize(this);

        _eManager = FindObjectOfType<EnemyManager>();
        if (_eManager)
            _eManager.Initialize(this);

        _hudManager = FindObjectOfType<HUDManager>();
        if (_hudManager)
            _hudManager.InitializeHUD(this);

        enemyType = EnemyType.None;
    }
    //Methods to handle order of execution for Update / LateUpdate
    private void Update()
    {
        audioHandler.CustomUpdate();
        ProcessGameTasks();
        _player.CustomUpdate();
        GeneralInput();
    }

    private void GeneralInput()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            SceneManager.LoadScene("Main Menu");
        }
    }

    private void LateUpdate()
    {
        _player.CustomLateUpdate();
    }
    /// <summary>
    /// function to handle game loop
    /// </summary>
    private void ProcessGameTasks()
    {
        if (!start)
        {
            startTimer += Time.deltaTime;
            if (startTimer >= startTime)
            {
                startTimer = 0.0f;
                _player.weapon.SetClipSize(weaponClipSize);
                start = true;
            }
        }
        else
        {
            // Make this into a seperate function if it gets too large
            if (enemyType == EnemyType.None)
            {
                enemyType = (EnemyType)UnityEngine.Random.Range(1, 4);
                _hudManager.enemyStatusBar.SetEnemyType(enemyType);
            }
            SpawnEnemyHandler();
            HandleRoundLogic();
            CheckObjectiveStatus();
        }
        PlayerWithinZone();
    }
    /// <summary>
    /// Function that contains the taasks for each round
    /// Rounds are completed by :
    ///     - Running out of time 
    ///     - Completing the task
    ///     
    /// </summary>
    private void HandleRoundLogic()
    {
        currRoundTime += Time.deltaTime;
        if (currRoundTime >= roundTime) // if the current time exceeds time limit.
        {
            checkRoundStatus = true;
        }
        else if (emptyClip) //if player has no more ammo left
        {
            checkRoundStatus = true;
        }
        else if (killedEnemies >= numOfEnemies) //if player has killed all enemies
        {
            checkRoundStatus = true;
            _roundNum++;
        }
    }
    /// <summary>
    /// Method to check the status after each round has been processed
    /// </summary>
    private void CheckObjectiveStatus()
    {
        if (checkRoundStatus)
        {
            StopCoroutine(SpawnEnemyCoroutine());
            _player.weapon.SetClipSize(0);
            _eManager.DeleteAllEnemies();
            _hudManager.enemyStatusBar.SetGroupActive(false);
            enemyType = EnemyType.None;
            start = false;
            checkRoundStatus = false;
            emptyClip = false;
            isSpawnRunning = false;
            currRoundTime = 0.0f;
            killedEnemies = 0;
            CalculateScore();
        }
    }
    /// <summary>
    /// Method for checking if the player is empty.
    /// Should be handled in an event.
    /// </summary>
    public void PlayerEmptyAmmoClip()
    {
        if(start) // check if the round is started first
            emptyClip = true;
    }
    /// <summary>
    /// Function to spawn enemies.
    /// Checks for coroutine
    /// </summary>
    private void SpawnEnemyHandler()
    {
        if (!isSpawnRunning)
        {
            StartCoroutine(SpawnEnemyCoroutine());
            isSpawnRunning = true;
        }
    }
    /// <summary>
    /// Coroutine for spawning enemies
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnEnemyCoroutine()
    {
        if (spawnPerSecond < 1)
            spawnPerSecond = 1;
        SpawnEnemies(spawnPerSecond);
        yield return new WaitForSeconds(1.0f);
        isSpawnRunning = false;
    }
    /// <summary>
    /// Function for spawning enemies
    /// </summary>
    /// <param name="num"></param>
    private void SpawnEnemies(int num)
    {
        if (num < 1) return; //error check

        for (int i = 0; i < num; i++)
        {
            _eManager.CreateNewEnemy(enemyHealthSize);
            // TODO :   Setup a way to add Enemy Definitions to Enemy Manager 
            //          based on round number.
        }
    }
    /// <summary>
    /// Method for updating our killed enemy score.
    /// Opted out of using events because of scope of game.
    /// </summary>
    public void EnemyWasKilled(Enemy target)
    {
        if (VerifyEnemyType(target))
        {
            killedEnemies++;
            _hudManager.enemyStatusBar.EnemyWasKilled();
        }
    }
    /// <summary>
    /// Check if the target was of the correct type
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool VerifyEnemyType(Enemy target)
    {
        return (target.type == enemyType) ? true : false;
    }
    /// <summary>
    /// Function for calculating score after each round
    /// </summary>
    private void CalculateScore()
    {
        score = killedEnemies / numOfEnemies * 100.0f;
        pass = (score < requiredScore) ? false : true;

        if (!pass)
        {
            _roundNum = 0;
        }
    }
    /// <summary>
    /// Check if player is within the playable area
    /// </summary>
    private void PlayerWithinZone()
    {
        float distance = (_player.transform.position - playerSpawnPoint.position).magnitude;
        
        withinZone = (distance < moveRadius) ? true : false;
    }
}
