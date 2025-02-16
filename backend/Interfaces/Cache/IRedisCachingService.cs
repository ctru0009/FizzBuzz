namespace backend.Interfaces.Cache
{
    public interface IRedisCachingService
    {
        public List<int>? GetData(string key);

        public void SetData(string key, List<int> data);

        public void RPushData(string key, int data);

        public bool IsExist(string key);
    }
}
