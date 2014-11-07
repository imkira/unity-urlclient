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
using System.Collections;
using System.Collections.Generic;

namespace URLClient
{
  public class HTTPClient : IDisposable
  {
    /// <summary>Default Connection Timeout for newly created connections.</summary>
    public static float DefaultConnectionTimeout = 60f;

    protected float _connectionTimeout;

    /// <summary>Connection Timeout</summary>
    public float ConnectionTimeout
    {
      get
      {
        return _connectionTimeout;
      }
      set
      {
        _connectionTimeout = value;
      }
    }

    protected HTTPRequest _request;

    /// <summary>Request object (creates one if it does not exist).</summary>
    public HTTPRequest Request
    {
      get
      {
        if (_request == null)
        {
          _request = new HTTPRequest();
        }
        return _request;
      }
      set
      {
        _request = value;
      }
    }

    protected HTTPResponse _response;

    /// <summary>Response object (if any).</summary>
    public IHTTPResponse Response
    {
      get
      {
        return _response;
      }
    }

    protected HTTPResponseHandler _responseHandler;

    /// <summary>Response handler object.</summary>
    public HTTPResponseHandler ResponseHandler
    {
      get
      {
        return _responseHandler;
      }
      set
      {
        _responseHandler = value;
      }
    }

    protected ConnectionState _state;

    /// <summary>Connection state.</summary>
    public ConnectionState State
    {
      get
      {
        return _state;
      }
    }

    protected Error _error;

    /// <summary>Error (null if none).</summary>
    public Error Error
    {
      get
      {
        return _error;
      }
    }

    /// <summary>Check whether the request has finished.</summary>
    public bool IsDone
    {
      get
      {
        if (CheckDone() == true)
        {
          return true;
        }

        if (_requestSent == false)
        {
          Start();
          return CheckDone();
        }

        Update();
        return CheckDone();
      }
    }

    protected uint _connectionID;

    /// <summary>Connection ID (internal use).</summary>
    public uint ConnectionID
    {
      get
      {
        return _connectionID;
      }
    }

    protected bool _requestSent;

    public HTTPClient(HTTPRequest request = null,
        HTTPResponseHandler responseHandler = null)
    {
      _connectionID = Bindings.INVALID_CONNECTION_ID;
      _state = ConnectionState.Initialized;
      _connectionTimeout = DefaultConnectionTimeout;
      _error = null;
      _requestSent = false;
      _request = request;
      _response = null;
      _responseHandler = responseHandler;
    }

    public HTTPClient(string url, HTTPHeaderList headers = null) :
      this(new HTTPRequest(url, headers))
    {
    }

    public HTTPClient(string url, string destinationPath,
        bool allowResume = true, HTTPHeaderList headers = null) :
      this(new HTTPRequest(url, headers))
    {
      _responseHandler = new HTTPResponseDownloadHandler(destinationPath,
          allowResume);
    }

    public HTTPClient SetRequest(HTTPRequest request)
    {
      _request = request;
      return this;
    }

    public HTTPClient SetResponseHandler(HTTPResponseHandler handler)
    {
      _responseHandler = handler;
      return this;
    }

    public HTTPClient SetConnectionTimeout(float timeout)
    {
      _connectionTimeout = timeout;
      return this;
    }

    public bool Start()
    {
      if ((_requestSent == true) ||
          (SanityCheckStart() == false))
      {
        return false;
      }

      _requestSent = true;

      string method = _request.method;

      if (string.IsNullOrEmpty(method) == true)
      {
        method = "GET";
      }

      _connectionID = Bindings._URLClientCreateHTTPConnection(method,
          _request.url.ToString(true, false),
          _responseHandler.cachePolicy, _connectionTimeout);

      if (_connectionID != Bindings.INVALID_CONNECTION_ID)
      {
        if (_request.contentHandler != null)
        {
          _request.contentHandler.OnWillSendRequest(this);
        }
        _responseHandler.OnWillSendRequest(this);
        SetAuthCredential();
        SetRequestHeaders();
        Bindings._URLClientSendRequest(_connectionID);
      }

      Update();
      return true;
    }

    protected bool SanityCheckStart()
    {
      string description = null;
      Error.KnownCode code = Error.KnownCode.ExtendedErrorMin;

      if (_disposed == true)
      {
        description = "Client is already disposed";
        code = Error.KnownCode.ClientAlreadyDisposedError;
      }
      else if (_request == null)
      {
        description = "Request is not set";
        code = Error.KnownCode.RequestNotSetError;
      }
      else if (_request.url == null)
      {
        description = "Request URL is invalid or not set";
        code = Error.KnownCode.RequestURLInvalid;
      }
      else
      {
        if ((_request.contentHandler != null) &&
            (_request.contentHandler.OnWillStart(this) == false))
        {
          description = "Request handler refused to start";
          code = Error.KnownCode.RequestHandlerDidNotStart;
        }
        else
        {
          if (_responseHandler == null)
          {
            _responseHandler = new HTTPResponseMemoryStreamHandler();
          }

          if (_responseHandler.OnWillStart(this) == false)
          {
            description = "Response handler refused to start";
            code = Error.KnownCode.ResponseHandlerDidNotStart;
          }
        }
      }

      if (description == null)
      {
        return true;
      }

      _error = new Error(Bindings.ERROR_DOMAIN, (long)code, description);
      return false;
    }

    protected void SetAuthCredential()
    {
      string user = _request.authUser;
      string password = _request.authPassword;

      if ((user != null) && (password != null))
      {
        Bindings._URLClientSetRequestAuthCredential(_connectionID,
            user, password);
      }
    }

    protected void SetRequestHeaders()
    {
      HTTPHeaderList headers = _request.headers;

      if (headers != null)
      {
        foreach (KeyValuePair<string, string> header in headers)
        {
          Bindings._URLClientSetRequestHeader(_connectionID,
              header.Key, header.Value);
        }
      }
    }

    public bool Cancel()
    {
      if ((_requestSent == false) || (CheckDone() == true))
      {
        return false;
      }

      ReleaseResources();
      return true;
    }

    public IEnumerator WaitUntilDone()
    {
      while (!IsDone)
      {
        yield return null;
      }
    }

    protected void UpdateStateAndError()
    {
      _state = Bindings.URLClientGetState(_connectionID);

      if (_error == null)
      {
        // if domain is not null, it means there is an error
        string domain = Bindings._URLClientGetErrorDomain(_connectionID);

        if (domain != null)
        {
          string description =
            Bindings._URLClientGetErrorDescription(_connectionID);
          long code = Bindings._URLClientGetErrorCode(_connectionID);
          _error = new Error(domain, code, description);
        }
      }
    }

    protected void Update()
    {
      UpdateStateAndError();

      _response = _responseHandler.OnDidUpdate(this, _response);

      if (_error == null)
      {
        _error = _responseHandler.Error;
      }

      if (CheckDone() == true)
      {
        ReleaseResources();
      }
    }

    protected bool CheckDone()
    {
      return ((_disposed == true) ||
          (_state < ConnectionState.Initialized) ||
          (_state >= ConnectionState.Finished) ||
          (_error != null));
    }

    public override string ToString()
    {
      return string.Format("HTTPClient<State={0},Error={1}," +
          "Request={2},Response{3}>", _state, _error,
          _request, _response);
    }

    #region IDisposable
    private bool _disposed = false;

    ~HTTPClient()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected void ReleaseResources()
    {
      uint connectionID = _connectionID;
      if (connectionID != Bindings.INVALID_CONNECTION_ID)
      {
        _connectionID = Bindings.INVALID_CONNECTION_ID;
        Bindings._URLClientDestroyConnection(connectionID);
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed == false)
      {
        _disposed = true;

        ReleaseResources();

        if (disposing)
        {
          if (_responseHandler != null)
          {
            _responseHandler.Dispose();
          }
          GC.SuppressFinalize(this);
        }
      }
    }
    #endregion
  }
}
