using BDG_ECS;
using ECSteroids;
using UnityEngine;

public class GamePlayState
{
    public static void Enter(ECSWorld world)
    {
        Debug.Log("entering gameplay state");

        world.MakeShip();

        int asteroidCount = 3;

        for (int i = 0; i < asteroidCount; ++i) {
            world.MakeInitialAsteroid();
        }
    }

    public static void Exit(ECSWorld world)
    {
    }
}

