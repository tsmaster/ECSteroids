using BDG_ECS;
using ECSteroids;
using UnityEngine;

public class TitleState
{
    public static void Enter(ECSWorld world)
    {
        world.QueueStateChange(ECSWorld.ECSteroidsGameState.Gameplay, 4.0f);
        TitleStateTag tag = new TitleStateTag();
        long eid = world.AddTextMessage("ECS-teroids", new Vector3(-10.0f, 1.0f, 0.0f), 2.0f, Color.green, 2.0f, tag);
        world.cmp_titleStateTags[eid] = tag;
    }

    public static void Exit(ECSWorld world)
    {
        foreach (TitleStateTag tag in world.cmp_titleStateTags.Values) {
            world.FlagEntityForDestruction(tag.EntityID);
        }
    }
}

