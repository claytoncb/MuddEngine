namespace MuddEngine.MuddEngine
{
    public static class ObjectHelpers {
        public static List<MuddObject> FlattenObjects(List<MuddObject> MuddObjects)
        {
            List<MuddObject> list = [];
            foreach(MuddObject muddObject in MuddObjects)
            {
                if (muddObject is MuddGroup)
                {
                    MuddGroup muddGroup = (MuddGroup)muddObject;
                    list.Add(muddGroup);
                    list.AddRange(FlattenObjects(muddGroup.Children));
                }
                else if (muddObject is MuddObject)
                {
                    list.Add(muddObject);
                }
            }
            return list.OrderBy(muddObject => -muddObject.GetPosition().Z)
                    .ThenBy(muddObject => muddObject.GetPosition().Y)
                    .ToList();;
        }
    }
}