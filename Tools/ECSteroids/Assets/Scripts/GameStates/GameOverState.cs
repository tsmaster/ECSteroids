using BDG_ECS;
using ECSteroids;

using UnityEngine;

public class GameOverState
{
    public static void Enter(ECSWorld world)
    {
        world.QueueStateChange(ECSWorld.ECSteroidsGameState.Title, 2.0f);
        GameplayStateTag tag = new GameplayStateTag();
        long eid = world.AddTextMessage("Game Over", new Vector3(-3.0f, 0.0f, 0.0f), 1.0f, Color.green, 1.0f, tag);
        world.cmp_gameplayStateTags[eid] = tag;
    }

    public static void Exit(ECSWorld world)
    {
        foreach (GameplayStateTag tag in world.cmp_gameplayStateTags.Values) {
            world.FlagEntityForDestruction(tag.EntityID);
        }
    }
}

