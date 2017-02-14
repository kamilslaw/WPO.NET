namespace WPO.Connection
{
    public class ExecuteResult<T>
    {
        public ExecuteResult(string key, T value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }

        public T Value { get; set; }
    }
}
