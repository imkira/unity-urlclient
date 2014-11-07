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

using System;
using System.Collections.Generic;

namespace URLClient
{
  public abstract class HTTPResponseHandler : IDisposable
  {
    /// <summary>Cache policy for response</summary>
    public CachePolicy cachePolicy;

    /// <summary>Acceptable status code ranges.</summary>
    public List<HTTPStatusCodeRange> acceptableStatusCodeRanges;

    /// <summary>Whether redirects should be followed.</summary>
    public bool allowFollowRedirects;

    /// <summary>Max redirect counts to be followed.</summary>
    public int maxRedirectCount;

    /// <summary>Whether invalid SSL certificates are allowed.</summary>
    public bool allowInvalidSSLCertificates;

    protected Error _error;

    /// <summary>Error (null if none).</summary>
    public Error Error
    {
      get
      {
        return _error;
      }
    }

    public HTTPResponseHandler()
    {
      cachePolicy = CachePolicy.UseProtocolCachePolicy;
      allowInvalidSSLCertificates = false;
      allowFollowRedirects = true;
      maxRedirectCount = -1;
      acceptableStatusCodeRanges = new List<HTTPStatusCodeRange>();
      _error = null;
    }

    #region Acceptable HTTP Status Codes Control
    public HTTPResponseHandler AddAcceptableStatusCode(long singleCode)
    {
      HTTPStatusCodeRange range = new HTTPStatusCodeRange(singleCode);
      return AddAcceptableStatusCodeRange(range);
    }

    public HTTPResponseHandler AddAcceptableStatusCodeRange(long fromCode,
        long toCode)
    {
      HTTPStatusCodeRange range = new HTTPStatusCodeRange(fromCode, toCode);
      return AddAcceptableStatusCodeRange(range);
    }

    public HTTPResponseHandler AddAcceptableStatusCodeRange(
        HTTPStatusCodeRange range)
    {
      if (acceptableStatusCodeRanges == null)
      {
        acceptableStatusCodeRanges = new List<HTTPStatusCodeRange>();
      }
      acceptableStatusCodeRanges.Add(range);
      return this;
    }
    #endregion

    #region Cache Policy
    public HTTPResponseHandler SetCachePolicy(CachePolicy policy)
    {
      cachePolicy = policy;
      return this;
    }
    #endregion

    #region SSL Validity Control
    public HTTPResponseHandler SetAllowInvalidSSLCertificates(bool allow)
    {
      allowInvalidSSLCertificates = allow;
      return this;
    }
    #endregion

    #region Redirect Control
    public HTTPResponseHandler SetAllowFollowRedirects(bool allow)
    {
      allowFollowRedirects = allow;
      return this;
    }

    public HTTPResponseHandler SetMaxRedirectCount(int maxCount)
    {
      maxRedirectCount = maxCount;
      if (maxRedirectCount >= 0)
      {
        allowFollowRedirects = true;
      }
      return this;
    }
    #endregion

    public virtual bool OnWillStart(HTTPClient client)
    {
      return true;
    }

    public virtual void OnWillSendRequest(HTTPClient client)
    {
      SetAllowFollowRedirects(client);
      SetAcceptableStatusCodes(client);
      SetAllowInvalidSSLCertificates(client);
    }

    protected void SetAllowFollowRedirects(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;

      Bindings._URLClientSetAllowFollowRedirects(
          connectionID, allowFollowRedirects, maxRedirectCount);
    }

    protected void SetAcceptableStatusCodes(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;

      if (acceptableStatusCodeRanges != null)
      {
        foreach (HTTPStatusCodeRange range in acceptableStatusCodeRanges)
        {
          Bindings._URLClientAddAcceptableResponseStatusCodeRange(
              connectionID, range.fromCode, range.toCode);
        }
      }
    }

    protected void SetAllowInvalidSSLCertificates(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;

      Bindings._URLClientSetAllowInvalidSSLCertificate(
          connectionID, allowInvalidSSLCertificates);
    }

    public virtual HTTPResponse OnDidUpdate(HTTPClient client,
        HTTPResponse response)
    {
      if (client.State < ConnectionState.ReceivingData)
      {
        return response;
      }

      if (response == null)
      {
        response = CreateResponse(client);
      }

      while (true)
      {
        UpdateContentData(client, response);

        if (CheckNeedsResponseReset(client) == false)
        {
          break;
        }

        response = ResetResponse(client, response);
      }

      return response;
    }

    protected virtual HTTPResponse CreateResponse(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;

      HTTPResponse response = new HTTPResponse();

      response.statusCode =
        Bindings._URLClientGetResponseStatusCode(connectionID);

      response.resumedContentLength =
        Bindings._URLClientGetResponseContentLengthResumed(connectionID);
      response.expectedReceiveContentLength =
        Bindings._URLClientGetResponseContentExpectedLength(connectionID);
      response.redirectCount =
        Bindings._URLClientGetResponseRedirectCount(connectionID);

      response.headers = CreateHeaders(client);

      return response;
    }

    protected HTTPHeaderList CreateHeaders(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;
      HTTPHeaderList headers = new HTTPHeaderList();
      string name, value;

      // read headers
      for (uint i = 0; i < (uint)int.MaxValue; ++i)
      {
        name = Bindings._URLClientGetResponseHeaderName(connectionID, i);
        if (name == null)
        {
          break;
        }
        value = Bindings._URLClientGetResponseHeaderValue(connectionID, i);
        if (value == null)
        {
          break;
        }

        headers[name] = value;
      }

      return headers;
    }

    protected virtual bool CheckNeedsResponseReset(HTTPClient client)
    {
      uint connectionID = client.ConnectionID;
      bool isDirty =
        Bindings._URLClientCheckAndResetResponseDirtyFlag(connectionID);

      return isDirty;
    }

    protected virtual HTTPResponse ResetResponse(HTTPClient client,
        HTTPResponse response)
    {
      response = CreateResponse(client);
      return response;
    }

    protected abstract void UpdateContentData(HTTPClient client,
        HTTPResponse response);

    #region IDisposable
    private bool _disposed = false;

    ~HTTPResponseHandler()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void ReleaseResources(bool disposing)
    {
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed == false)
      {
        _disposed = true;

        ReleaseResources(disposing);

        if (disposing)
        {
          GC.SuppressFinalize(this);
        }
      }
    }
    #endregion
  }
}
