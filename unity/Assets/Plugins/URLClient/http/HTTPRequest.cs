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
  public class HTTPRequest
  {
    /// <summary>GET, HEAD, POST, PUT, DELETE, ...</summary>
    public string method;

    /// <summary>HTTP/HTTPS URL for the request.</summary>
    public URL url;

    /// <summary>Username to use for authentication.</summary>
    public string authUser;

    /// <summary>Password to use for authentication.</summary>
    public string authPassword;

    /// <summary>Headers to be sent to the server.</summary>
    public HTTPHeaderList headers;

    /// <summary>Content handler for this request.</summary>
    public HTTPRequestContentHandler contentHandler;

    public HTTPRequest() : this(null)
    {
    }

    public HTTPRequest(string url, HTTPHeaderList headers = null)
    {
      method = "GET";
      SetURL(url);

      if (headers == null)
      {
        headers = new HTTPHeaderList();
      }

      authUser = null;
      authPassword = null;
      contentHandler = null;
    }

    #region HTTP url
    public HTTPRequest SetURL(URL url)
    {
      if ((url == null) || ((url.Scheme != "http") && (url.Scheme != "https")))
      {
        url = null;
      }
      this.url = url;
      return this;
    }

    public HTTPRequest SetURL(string url)
    {
      return SetURL(URL.Parse(url));
    }

    public HTTPRequest AppendQueryParameter(string name, string value,
        bool escape = true)
    {
      url.AppendQueryParameter(name, value, escape);
      return this;
    }
    #endregion

    #region HTTP method
    public HTTPRequest SetMethod(string method)
    {
      this.method = method;
      return this;
    }

    public HTTPRequest Get()
    {
      return SetMethod("GET");
    }

    public HTTPRequest Head()
    {
      return SetMethod("HEAD");
    }

    public HTTPRequest Post()
    {
      return SetMethod("POST");
    }

    public HTTPRequest Post(byte[] content, string contentType = null)
    {
      SetContentBytes(content, contentType);
      return SetMethod("POST");
    }

    public HTTPRequest Post(string content, string contentType = null,
        Encoding enc = null)
    {
      SetContentString(content, contentType);
      return SetMethod("POST");
    }

    public HTTPRequest Put()
    {
      return SetMethod("PUT");
    }

    public HTTPRequest Delete()
    {
      return SetMethod("DELETE");
    }

    public HTTPRequest Options()
    {
      return SetMethod("DELETE");
    }
    #endregion

    #region HTTP content
    public HTTPRequest SetContentHandler(HTTPRequestContentHandler handler)
    {
      contentHandler = handler;
      return this;
    }

    public HTTPRequest SetContentFromPath(string srcPath,
        string contentType = null)
    {
      contentHandler = new HTTPRequestContentUploadHandler(srcPath);
      if (contentType != null)
      {
        SetHeader("Content-Type", contentType);
      }
      return this;
    }

    public HTTPRequest SetContentBytes(byte[] content, string contentType = null)
    {
      contentHandler = new HTTPRequestContentBytesHandler(content);
      if (contentType != null)
      {
        SetHeader("Content-Type", contentType);
      }
      return this;
    }

    public HTTPRequest SetContentString(string content,
        string contentType = null, Encoding enc = null)
    {
      if (content == null)
      {
        return SetContentBytes(null, contentType);
      }
      if (enc == null)
      {
        enc = Encoding.UTF8;
      }
      return SetContentBytes(enc.GetBytes(content), contentType);
    }
    #endregion

    #region HTTP header
    public HTTPRequest SetHeader(string name, string value)
    {
      if (headers == null)
      {
        headers = new HTTPHeaderList();
      }
      headers[name] = value;
      return this;
    }

    public HTTPRequest AppendCookie(string name, string value,
        bool escape = true)
    {
      string cookie = (headers == null) ? null : headers["Cookie"];

      if (escape == true)
      {
        name = HTTPUtils.URLEncode(name);
        value = HTTPUtils.URLEncode(value);
      }

      if (string.IsNullOrEmpty(cookie))
      {
        cookie = name + "=" + value;
      }
      else
      {
        cookie += "; " + name + "=" + value;
      }

      return SetHeader("Cookie", cookie);
    }

    public HTTPRequest AppendRange(ulong firstBytePos, ulong lastBytePos)
    {
      string range = (headers == null) ? null : headers["Range"];

      if (string.IsNullOrEmpty(range))
      {
        range = "bytes=" + firstBytePos + "-" + lastBytePos;
      }
      else
      {
        range += "," + firstBytePos + "-" + lastBytePos;
      }

      return SetHeader("Range", range);
    }

    public HTTPRequest AppendRange(ulong firstBytePos)
    {
      string range = (headers == null) ? null : headers["Range"];

      if (string.IsNullOrEmpty(range))
      {
        range = "bytes=" + firstBytePos + "-";
      }
      else
      {
        range += "," + firstBytePos + "-";
      }

      return SetHeader("Range", range);
    }

    public HTTPRequest AppendRangeSuffix(ulong suffixLength)
    {
      string range = (headers == null) ? null : headers["Range"];

      if (string.IsNullOrEmpty(range))
      {
        range = "bytes=-" + suffixLength;
      }
      else
      {
        range += ",-" + suffixLength;
      }

      return SetHeader("Range", range);
    }

    public HTTPRequest RemoveHeader(string name)
    {
      if (headers != null)
      {
        headers.Remove(name);
      }
      return this;
    }

    public HTTPRequest ClearHeaders()
    {
      if (headers != null)
      {
        headers.Clear();
      }
      return this;
    }

    public HTTPRequest SetUserAgent(string contentType)
    {
      return SetHeader("User-Agent", contentType);
    }

    public HTTPRequest SetContentType(string type, string subtype,
        string parameter = null)
    {
      string mediaType = type + "/" + subtype;

      if (string.IsNullOrEmpty(parameter) == false)
      {
        mediaType += "; " + parameter;
      }

      return SetHeader("Content-Type", mediaType);
    }
    #endregion

    #region Authentication
    public HTTPRequest SetAuthCredential(string user, string password)
    {
      authUser = user;
      authPassword = password;
      return this;
    }
    #endregion

    public override string ToString()
    {
      return string.Format("HTTPRequest<method={0},url={1},headers={2}>",
          method, ((url == null) ? "" : url.ToString(true, false)), headers);
    }
  }
}
