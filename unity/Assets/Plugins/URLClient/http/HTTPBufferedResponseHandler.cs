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

namespace URLClient
{
  public abstract class HTTPBufferedResponseHandler : HTTPResponseHandler
  {
    protected const ulong RESPONSE_BUFFER_SIZE = (ulong)(64 * 1024);
    protected byte[] _contentDataBuffer;

    public HTTPBufferedResponseHandler() : base()
    {
      _contentDataBuffer = null;
    }

    protected override void UpdateContentData(HTTPClient client,
        HTTPResponse response)
    {
      uint connectionID = client.ConnectionID;
      ulong length =
        Bindings._URLClientGetPendingResponseContentLength(connectionID);

      // no data pending?
      if (length <= 0)
      {
        return;
      }

      ulong copied;

      // buffer read pending data
      do
      {
        if (_contentDataBuffer == null)
        {
          _contentDataBuffer = new byte[RESPONSE_BUFFER_SIZE];
        }

        copied = Bindings.URLClientMovePendingResponseContent(connectionID,
            _contentDataBuffer, RESPONSE_BUFFER_SIZE);

        if (copied <= (ulong)0)
        {
          break;
        }

        OnDidReceiveData(response, _contentDataBuffer, copied);
        response.receivedContentLength += copied;
      } while (copied == RESPONSE_BUFFER_SIZE);
    }

    protected abstract void OnDidReceiveData(HTTPResponse response,
        byte[] buffer, ulong length);
  }
}
