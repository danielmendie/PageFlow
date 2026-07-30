namespace PageFlow.Blazor.Tests.Mocks
{
    public sealed class MockKeyValueStore : ILocalStorageProvider
    {
        private readonly Dictionary<string, object?> _store = new();

        public Task SetItemAsync<T>(string key, T value)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task<T?> GetItemAsync<T>(string key)
        {
            _store.TryGetValue(key, out var v);
            return Task.FromResult((T?)v);
        }

        public Task RemoveItemAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }
    }
}
