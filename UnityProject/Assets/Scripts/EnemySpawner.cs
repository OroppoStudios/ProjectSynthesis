using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Warner;

public class EnemySpawner : MonoBehaviour
    {
    #region MEMBER FIELDS

    [NonSerialized]
    public int spawnCount = 0;

    [NonSerialized]
    public List<LevelEditor.SpawnableEnemyType> enemies = new List<LevelEditor.SpawnableEnemyType>();

    private List<LevelEditor.EnemyType> toSpawn = new List<LevelEditor.EnemyType>();

    #endregion


    #region INIT

    public void init()
        {
        toSpawn.Clear();

        float highest;
        float chance;
        int index;

        for (int x = 0;x < spawnCount; x++)
            {
            index = 0;
            highest = 0;

            //go through the enemies and get a percent chance and add the one set on the editor, this way well get the biggest random chance modified with the set on the editor
            for (int i = 0;i < enemies.Count;i++)
                {
                chance = UnityEngine.Random.value + (enemies[i].chance * 0.01f);
                if (chance > highest)
                    {
                    index = i;
                    highest = chance;
                    }
                }

            toSpawn.Add(enemies[index].enemy);
            }

        int enemyIndex;
        for (int i = 0; i < toSpawn.Count; i++)
            {
            enemyIndex = -1;
            switch (toSpawn[i])
                {
                case LevelEditor.EnemyType.Clone:
                    enemyIndex = 1;
                    break;
                case LevelEditor.EnemyType.Sentry:
                    enemyIndex = 2;
                break;
                case LevelEditor.EnemyType.Gunner:
                    enemyIndex = 3;
                break;
                }

            if (enemyIndex != -1)
                {
                Character character = Director.instance.spawnCharacter(enemyIndex, transform.position);

                if (character)
                    character.movements.groundLayer = LevelMaster.instance.layers.enemiesGround;
                }
            }
        }

    #endregion
    }
