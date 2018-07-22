using BDG_ECS;
using ECSteroids;
using UnityEngine;

public class BootState
{
    public static void Enter(ECSWorld world)
    {
        world.QueueStateChange(ECSWorld.ECSteroidsGameState.Title, 2.0f);
        BootStateTag tag = new BootStateTag();
        long eid = world.AddTextMessage("BDGOS ][", new Vector3(-3.0f, 22.0f, 0.0f), 1.0f, Color.green, 1.0f, tag);
        world.cmp_bootStateTags[eid] = tag;
    }

    public static void Exit(ECSWorld world)
    {
        foreach (BootStateTag tag in world.cmp_bootStateTags.Values) {
            world.FlagEntityForDestruction(tag.EntityID);
        }
    }
}

