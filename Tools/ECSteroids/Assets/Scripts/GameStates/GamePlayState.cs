using BDG_ECS;
using ECSteroids;
using UnityEngine;

public class GamePlayState
{
    public static void Enter(ECSWorld world)
    {
        Debug.Log("entering gameplay state");

        world.MakeShip();

        world.PopulateForWave();
    }

    public static void Exit(ECSWorld world)
    {
    }
}

