using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.State;

namespace Nekoyume.Model
{
    public class CollectionMap : IState, IDictionary<int, int>
    {
        private readonly Dictionary<int, int> _dictionary = new Dictionary<int, int>();

        public CollectionMap()
        {
        }

        public CollectionMap(Bencodex.Types.Dictionary serialized) : base()
        {
            _dictionary = serialized.ToDictionary(
                kv => kv.Key.ToInt(),
                kv => kv.Value.ToInt()
            );
        }

        public IValue Serialize()
        {
            return new Bencodex.Types.Dictionary(_dictionary.Select(kv =>
                new KeyValuePair<IKey, IValue>((Text) kv.Key.ToString(), (Text) kv.Value.ToString())));
        }

        public void Add(KeyValuePair<int, int> pair)
        {
            if (_dictionary.ContainsKey(pair.Key))
            {
                _dictionary[pair.Key] += pair.Value;
            }
            else
            {
                _dictionary[pair.Key] = pair.Value;
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<int, int> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<int, int>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, int> item)
        {
            return _dictionary.Remove(item.Key);
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(int key, int value)
        {
            Add(new KeyValuePair<int, int>(key, value));
        }

        public bool ContainsKey(int key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Remove(int key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(int key, out int value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public int this[int key]
        {
            get => _dictionary[key];
            set => Add(new KeyValuePair<int, int>(key, value));
        }

        public ICollection<int> Keys => _dictionary.Keys;
        public ICollection<int> Values => _dictionary.Values;
    }
}
