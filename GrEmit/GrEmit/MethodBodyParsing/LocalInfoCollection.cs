namespace GrEmit.MethodBodyParsing
{
    internal class LocalInfoCollection : Collection<LocalInfo>
    {
        internal LocalInfoCollection()
        {
        }

        internal LocalInfoCollection(int capacity)
            : base(capacity)
        {
        }

        protected override void OnAdd(LocalInfo item, int index)
        {
            item.LocalIndex = index;
        }

        protected override void OnInsert(LocalInfo item, int index)
        {
            item.LocalIndex = index;

            for(int i = index; i < size; i++)
                items[i].LocalIndex = i + 1;
        }

        protected override void OnSet(LocalInfo item, int index)
        {
            item.LocalIndex = index;
        }

        protected override void OnRemove(LocalInfo item, int index)
        {
            item.LocalIndex = -1;

            for(int i = index + 1; i < size; i++)
                items[i].LocalIndex = i - 1;
        }
    }
}