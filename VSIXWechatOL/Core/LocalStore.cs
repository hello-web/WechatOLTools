using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXWechatOL.Core
{
    public class LocalStore
    {
        private static object _obj = new object();
        private static LocalStore _store;
        public static LocalStore Store
        {
            get
            {
                if (_store == null)
                {
                    lock (_obj)
                    {
                        if (_store == null)
                        {
                            _store = new LocalStore();
                        }
                    }
                }
                return _store;
            }
        }

        private ConcurrentDictionary<string, List<string>> _innerData = null;
        private ConcurrentQueue<string> _queue = null;
        private int _maxNum = 10000;

        private LocalStore()
        {
            _innerData = new ConcurrentDictionary<string, List<string>>();
            _queue = new ConcurrentQueue<string>();
        }

        public List<string> Get(string key)
        {
            var result = new List<string>();
            if (_innerData.TryGetValue(key, out result))
                return result;
            else
                return null;
        }

        public void Set(string key, string val)
        {
            Set(key, new List<string>() { val });
        }

        public void Set(string key, List<string> vals)
        {
            if (_innerData.ContainsKey(key))
            {
                _innerData[key].AddRange(vals);
            }
            else
            {
                if (_queue.Count >= _maxNum)
                {
                    if (_queue.TryDequeue(out string k))
                        _innerData.TryRemove(key, out List<string> val);
                }

                _innerData.TryAdd(key, vals);
                _queue.Enqueue(key);
            }
        }

        public void Clear()
        {
            _innerData.Clear();
            _queue = new ConcurrentQueue<string>();
        }

    }
}
