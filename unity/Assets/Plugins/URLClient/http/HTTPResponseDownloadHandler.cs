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

namespace URLClient
{
  public class HTTPResponseDownloadHandler : HTTPResponseHandler
  {
    protected string _destinationPath;
    public string DestinationPath
    {
      get
      {
        return _destinationPath;
      }
      set
      {
        _destinationPath = value;
      }
    }

    protected bool _allowResume;
    public bool AllowResume
    {
      get
      {
        return _allowResume;
      }
      set
      {
        _allowResume = value;
      }
    }

    public HTTPResponseDownloadHandler(string destinationPath = null,
        bool allowResume = true)
    {
      _destinationPath = destinationPath;
      _allowResume = allowResume;
    }

    public override bool OnWillStart(HTTPClient client)
    {
      return (string.IsNullOrEmpty(_destinationPath) == false);
    }

    public override void OnWillSendRequest(HTTPClient client)
    {
      if (_allowResume == true)
      {
        AddAcceptableStatusCode(200);
        AddAcceptableStatusCode(206);
        AddAcceptableStatusCode(416);
      }

      base.OnWillSendRequest(client);

      uint connectionID = client.ConnectionID;
      Bindings._URLClientSetResponseContentDestination(connectionID,
          _destinationPath, _allowResume);
    }

    protected override HTTPResponse CreateResponse(HTTPClient client)
    {
      HTTPResponse response = base.CreateResponse(client);
      response.supportsContentFilePath = true;
      response.contentFilePath = _destinationPath;
      return response;
    }

    protected override void UpdateContentData(HTTPClient client,
        HTTPResponse response)
    {
      uint connectionID = client.ConnectionID;
      response.receivedContentLength =
        Bindings._URLClientGetResponseContentLengthRead(connectionID);
    }
  }
}
