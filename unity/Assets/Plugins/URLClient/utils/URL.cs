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

using System.Text;

namespace URLClient
{
  public class URL
  {
    protected string _raw = null;
    public string RawString
    {
      get
      {
        return _raw;
      }
    }

    protected string _scheme = null;
    public string Scheme
    {
      get
      {
        return _scheme;
      }
    }

    protected string _authority = null;
    public string Authority
    {
      get
      {
        return _authority;
      }
    }

    protected string _path = null;
    public string Path
    {
      get
      {
        return _path;
      }
    }

    protected string _query = null;
    public string Query
    {
      get
      {
        return _query;
      }
    }

    protected string _fragment = null;
    public string Fragment
    {
      get
      {
        return _fragment;
      }
    }

    public string ToString(bool includeQuery, bool includeFragment)
    {
      StringBuilder builder = new StringBuilder(_scheme);

      builder.Append("://");

      if (_authority != null)
      {
        builder.Append(_authority);
      }

      builder.Append('/').Append(_path);

      if ((includeQuery == true) && (_query != null))
      {
        builder.Append('?').Append(_query);
      }

      if ((includeFragment == true) && (_fragment != null))
      {
        builder.Append('#').Append(_fragment);
      }

      return builder.ToString();
    }

    public override string ToString()
    {
      return ToString(true, true);
    }

    public URL AppendQueryParameter(string name, string value,
        bool escape = true)
    {
      if (_scheme == "file")
      {
        return this;
      }

      if (escape == true)
      {
        name = HTTPUtils.URLEncode(name);
        value = HTTPUtils.URLEncode(value);
      }

      if (string.IsNullOrEmpty(_query) == true)
      {
        _query = name + "=" + value;
      }
      else
      {
        _query += "&" + name + "=" + value;
      }

      return this;
    }

    /// <summary>Parse URL components (based on RFC3986).</summary>
    public static URL Parse(string str, string defaultScheme = "http")
    {
      if (string.IsNullOrEmpty(str) == true)
      {
        return null;
      }

      char[] chars;
      int pos;

      URL url = new URL();
      url._raw = str;

      // scheme
      pos = str.IndexOf(':');

      if (pos > 0)
      {
        url._scheme = str.Substring(0, pos).ToLower();
        str = str.Substring(pos + 1);
      }

      // authority
      pos = -1;
      if ((str.StartsWith("//") == true) &&
          (url._scheme != null) && (url._scheme != "file"))
      {
        pos = 2;
      }
      else if (str.StartsWith("/") == true)
      {
        if ((url._scheme != null) && (url._scheme != "file"))
        {
          return null;
        }

        url._scheme = "file";
      }
      else
      {
        if (string.IsNullOrEmpty(defaultScheme) == true)
        {
          return null;
        }

        url._scheme = defaultScheme.ToLower();
        pos = 0;
      }

      // file protocol does not have query nor fragment
      if (url._scheme == "file")
      {
        // path
        url.ParsePath(str);
        return url;
      }

      // validate scheme
      chars = new char[] {':', '/', '?', '#' };
      if ((string.IsNullOrEmpty(url._scheme) == true) ||
          (url._scheme.IndexOfAny(chars) >= 0))
      {
        return null;
      }

      if (pos >= 0)
      {
        str = url.ParseAuthority(str, pos);
      }

      // validate authority
      if (string.IsNullOrEmpty(url._authority) == true)
      {
        return null;
      }

      // fragment
      pos = str.IndexOf('#');

      if (pos >= 0)
      {
        url._fragment = str.Substring(pos + 1);
        str = str.Substring(0, pos);
      }

      // query
      pos = str.IndexOf('?');

      if (pos >= 0)
      {
        url._query = str.Substring(pos + 1);
        str = str.Substring(0, pos);
      }

      // path
      url.ParsePath(str);
      return url;
    }

    protected void ParsePath(string str)
    {
      int len = str.Length;
      for (int i = 0; i < len; ++i)
      {
        if (str[i] != '/')
        {
          _path = str.Substring(i);
          return;
        }
      }

      _path = string.Empty;
    }

    protected string ParseAuthority(string str, int startPos)
    {
      char[] chars = new char[] {'/', '?', '#'};
      int pos = str.IndexOfAny(chars, startPos);

      if (pos >= startPos)
      {
        _authority = str.Substring(startPos, pos - startPos);
        str = str.Substring(pos);
      }
      else
      {
        _authority = str.Substring(startPos);
        str = string.Empty;
      }

      return str;
    }
  }
}
