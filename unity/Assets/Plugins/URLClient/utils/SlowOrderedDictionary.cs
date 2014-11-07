/*
 * Copyright (c) 2013 Mario Freitas (imkira@gmail.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


using System.Collections;
using System.Collections.Generic;

namespace URLClient
{
  public interface ISlowOrderedDictionary<K, V> :
    IEnumerable<KeyValuePair<K, V>>, IEnumerable
  {
    int Count { get; }
    bool IsEmpty { get; }
    V this[K key] { get; }
    int IndexOf(K key);
    bool Contains(K key);
  }

  public class SlowOrderedDictionary<K, V> : ISlowOrderedDictionary<K, V>
  {
    protected List<KeyValuePair<K, V>> _list;
    protected IEqualityComparer<K> _comparer;

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {
      return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public int Count
    {
      get
      {
        return _list.Count;
      }
    }

    public bool IsEmpty
    {
      get
      {
        return (_list.Count <= 0);
      }
    }

    public V this[K key]
    {
      get
      {
        int index = IndexOf(key);
        if (index < 0)
        {
          return default(V);
        }
        return _list[index].Value;
      }
      set
      {
        int index = IndexOf(key);
        if (index < 0)
        {
          _list.Add(new KeyValuePair<K, V>(key, value));
        }
        else
        {
          _list[index] = new KeyValuePair<K, V>(key, value);
        }
      }
    }

    public SlowOrderedDictionary(int capacity = -1) :
      this(capacity, EqualityComparer<K>.Default)
    {
    }

    public SlowOrderedDictionary(IEqualityComparer<K> comparer) :
      this(-1, comparer)
    {
    }

    public SlowOrderedDictionary(int capacity, IEqualityComparer<K> comparer)
    {
      _comparer = comparer;

      if (capacity >= 0)
      {
        _list = new List<KeyValuePair<K, V>>(capacity);
      }
      else
      {
        _list = new List<KeyValuePair<K, V>>();
      }
    }

    public int IndexOf(K key)
    {
      for (int i = 0, len = _list.Count; i < len; ++i)
      {
        if (_comparer.Equals(key, _list[i].Key))
        {
          return i;
        }
      }

      return -1;
    }

    public bool Contains(K key)
    {
      return (IndexOf(key) >= 0);
    }

    public bool Remove(K key)
    {
      int index = IndexOf(key);

      if (index < 0)
      {
        return false;
      }

      _list.RemoveAt(index);
      return true;
    }

    public void Clear()
    {
      _list.Clear();
    }
  }
}
