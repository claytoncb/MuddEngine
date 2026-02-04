using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class MuddGroup : MuddObject
    {
        internal List<MuddObject> Children;
        public MuddGroup(string Id, Vector3 Position) : base(Id, Position)
        {
            Children = [];
        }
        public void AddChild(MuddObject muddObject)
        {
            muddObject.Parent = this;
            Children.Add(muddObject);
        }
        public void AddChildren(List<MuddObject> muddObjects)
        {
            Children.AddRange(muddObjects);
        }
        public void RemoveChild(MuddObject toRemove)
        {
            toRemove.Parent = null;
            Children = Children.Where(muddObject=>muddObject.Id!=toRemove.Id).ToList();
        }
        public void RemoveChildren(List<MuddObject> muddObjects)
        {
            foreach (MuddObject toRemove in muddObjects)
            {
                toRemove.Parent = null;
                Children = Children.Where(muddObject=>muddObject.Id!=toRemove.Id).ToList();
            }
        }
        public override void Update(float dt, float t)
        {
            base.Update(dt, t);
            foreach(MuddObject Child in Children)
            {
                Child.Update(dt, t);
            }
        }
    }
}