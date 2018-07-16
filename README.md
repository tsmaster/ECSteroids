# ECS-teroids
In a world filled with space rocks, one ship can make a difference. 

Sometime in June of 2018, I read a description of the
Entity/Component/System architecture for games. The interesting bit is
that this provides a fairly strict structure for data and logic: data
is put into components, which are like C-structs: just semantic
packets of data. Logic is put into "systems", which have no
fields. Systems operate on components to get their job done, but they
don't keep stuff around, and systems don't "own" their components, nor
do components have "member methods".

So, this is an exercise in building my own ECS-architecture, with a
simple-ish game about a ship blowing up rocks in space.

It's resting on top of Unity, which makes for an uncomfortable
mismatch, as Unity has its own architecture with components, but for
our purposes, our entities, components, and systems should be clearly
distinct from what Unity provides.



## References:

https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system


