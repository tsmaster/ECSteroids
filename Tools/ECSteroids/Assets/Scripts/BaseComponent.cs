using System;

namespace BDG_ECS
{
    using EntityID = Int64;

    public class BaseComponent
    {
        public EntityID EntityID;

        public BaseComponent ()
        {
        }

        public virtual String Serialize() {
            return "bogus";
        }

        public virtual bool Deserialize(String s) {
            return false;
        }

        public virtual void Reset() {
        }
    }
}

