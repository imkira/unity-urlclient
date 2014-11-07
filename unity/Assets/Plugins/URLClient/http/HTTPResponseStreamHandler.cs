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

using System.IO;

namespace URLClient
{
  public abstract class HTTPResponseStreamHandler : HTTPBufferedResponseHandler
  {
    protected Stream _stream;
    protected BinaryWriter _writer;

    public HTTPResponseStreamHandler(Stream stream) : base()
    {
      _stream = stream;
      _writer = null;
    }

    public HTTPResponseStreamHandler() : this(null)
    {
    }

    protected override HTTPResponse CreateResponse(HTTPClient client)
    {
      HTTPResponse response = base.CreateResponse(client);
      response.supportsContentStream = true;
      return response;
    }

    protected virtual void CreateStream(HTTPResponse response)
    {
      response.contentStream = _stream;
      if (_writer != null)
      {
        _writer.Close();
        _writer = new BinaryWriter(_stream);
      }
    }

    protected override void OnDidReceiveData(HTTPResponse response,
        byte[] buffer, ulong length)
    {
      int len = (int)length;

      if (_writer == null)
      {
        if (_stream == null)
        {
          CreateStream(response);
        }
        _writer = new BinaryWriter(_stream);
      }

      _writer.Write(buffer, 0, len);
    }

    #region IDisposable
    protected override void ReleaseResources(bool disposing)
    {
      base.ReleaseResources(disposing);

      if (disposing)
      {
        _contentDataBuffer = null;

        if (_stream != null)
        {
          _stream.Dispose();
        }

        if (_writer != null)
        {
          _writer.Close();
          _writer = null;
        }
      }
    }
    #endregion
  }
}
