﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Transform spawnZoneCenter;
    public float spawnZoneRadius;
    //Raycast from y height to ground to get height position for spawning object
    private float raycastHeight = 10f;
    public float raycastDistance = 15f;
    [SerializeField] List<Enemy> enemyList;
    GameManager _gameManager;
    public GameManager gameManager { get { return _gameManager; } }
    public List<GameObject> enemyPrefabs;
    public float spawnHeightOffset = 0.15f;

    private bool groundCheck = false;
    private int groundMask;
    private bool deleteAllEnemies = false;
    /// <summary>
    /// Initialization our our manager class
    /// </summary>
    /// <param name="manager"></param>
    public void Initialize(GameManager manager)
    {
        _gameManager = manager;
        enemyList = new List<Enemy>();
        groundMask = 1 << LayerMask.NameToLayer("Ground");
    }
    /// <summary>
    /// Create a new enemy during run-time
    /// </summary>
    public void CreateNewEnemy(int health)
    {
        int choice = 0;
        choice = Random.Range(0, enemyPrefabs.Count);

        Enemy newEnemy = Instantiate(enemyPrefabs[choice], GetSpawnPosition(), enemyPrefabs[choice].transform.rotation).GetComponent<Enemy>();
        newEnemy.Initialize(this, health);
        
        enemyList.Add(newEnemy);
    }
    /// <summary>
    /// Delete an Enemy from our list. Remove all references to object before deletion.
    /// </summary>
    /// <param name="target"></param>
    public void DeleteEnemy(Enemy target)
    {
        _gameManager.EnemyWasKilled(target);
        enemyList.Remove(target);
        Destroy(target.gameObject);
    }
    /// <summary>
    /// Deletes all enemies 
    /// TODO : Make a better enemy deletion system.
    /// Process currently points to the enemy, then points back to itself
    /// </summary>
    public void DeleteAllEnemies()
    {
        foreach (Enemy e in enemyList)
        {
            e.DeleteAllEnemyEvent();
        }
        enemyList.Clear();
    }
    /// <summary>
    /// Function calculates a position from casting a ray from x height and hitting the ground.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetSpawnPosition()
    {
        Vector3 newPosition = Vector3.zero;
        Vector3 tempOfPosition = Vector3.zero;
        float hieght = 1f;

        newPosition = Random.insideUnitSphere * spawnZoneRadius + spawnZoneCenter.position;
        tempOfPosition = new Vector3(newPosition.x, raycastHeight, newPosition.y);

        RaycastHit hit;

        do
        {
            if (Physics.Raycast(tempOfPosition, Vector3.down, out hit, raycastDistance, groundMask))
            {
                groundCheck = true;
                hieght = hit.point.y + spawnHeightOffset;
            }
            else
            {
                groundCheck = false;
            }
        } while (!groundCheck);

        newPosition = new Vector3(newPosition.x, hieght, newPosition.z);
        return newPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if(spawnZoneCenter)
            Gizmos.DrawWireSphere(spawnZoneCenter.position, spawnZoneRadius);
    }
}
