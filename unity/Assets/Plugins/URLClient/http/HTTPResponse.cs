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
using System.Text;

namespace URLClient
{
  public interface IHTTPResponse
  {
    long StatusCode { get; }
    IHTTPHeaderList Headers { get; }

    ulong ExpectedReceiveContentLength { get; }
    ulong ResumedContentLength { get; }
    ulong ReceivedContentLength { get; }
    ulong ExpectedAcquiredContentLength { get; }
    ulong AcquiredContentLength { get; }

    bool SupportsContentStream { get; }
    Stream ContentStream { get; }

    bool SupportsContentMemoryStream { get; }
    MemoryStream ContentMemoryStream { get; }

    bool SupportsContentFilePath { get; }
    string ContentFilePath { get; }

    byte[] ContentToBytes();
    string ContentToString(Encoding enc = null);

    int RedirectCount { get; }

    bool IsProgressAvailable { get; }
    float Progress { get; }
    float ReceiveProgress { get; }
  }

  public class HTTPResponse : IHTTPResponse
  {
    /// <summary>Status code.</summary>
    public long statusCode;
    public long StatusCode
    {
      get
      {
        return statusCode;
      }
    }

    /// <summary>Headers received.</summary>
    public HTTPHeaderList headers;
    public IHTTPHeaderList Headers
    {
      get
      {
        return headers;
      }
    }

    /// <summary>Total expected content length to receive.</summary>
    public long expectedReceiveContentLength;
    public ulong ExpectedReceiveContentLength
    {
      get
      {
        if (expectedReceiveContentLength < (long)0)
        {
          return (ulong)0;
        }
        return (ulong)expectedReceiveContentLength;
      }
    }

    /// <summary>Total content length resumed.</summary>
    public ulong resumedContentLength;
    public ulong ResumedContentLength
    {
      get
      {
        return resumedContentLength;
      }
    }

    /// <summary>Total content length received.</summary>
    public ulong receivedContentLength;
    public ulong ReceivedContentLength
    {
      get
      {
        return receivedContentLength;
      }
    }

    /// <summary>Total expected acquire content length.</summary>
    public ulong ExpectedAcquiredContentLength
    {
      get
      {
        if (expectedReceiveContentLength < (long)0)
        {
          return (ulong)0;
        }
        ulong total = (ulong)expectedReceiveContentLength +
          resumedContentLength;
        if (total < resumedContentLength)
        {
          return ulong.MaxValue;
        }
        return total;
      }
    }

    /// <summary>Total content length acquired (resumed + received).</summary>
    public ulong AcquiredContentLength
    {
      get
      {
        ulong total = receivedContentLength + resumedContentLength;
        if (total < resumedContentLength)
        {
          return ulong.MaxValue;
        }
        return total;
      }
    }

    /// <summary>Whether ContentStream is supported.</summary>
    public bool supportsContentStream;
    public bool SupportsContentStream
    {
      get
      {
        return supportsContentStream;
      }
    }

    /// <summary>Stream with received content.</summary>
    public Stream contentStream;
    public Stream ContentStream
    {
      get
      {
        return contentStream;
      }
    }

    /// <summary>Whether ContentMemoryStream is supported.</summary>
    public bool supportsContentMemoryStream;
    public bool SupportsContentMemoryStream
    {
      get
      {
        return supportsContentMemoryStream;
      }
    }

    /// <summary>MemoryStream with received content.</summary>
    public MemoryStream ContentMemoryStream
    {
      get
      {
        if (supportsContentMemoryStream == false)
        {
          return null;
        }
        return (MemoryStream)contentStream;
      }
    }

    /// <summary>Whether ContentFilePath is supported.</summary>
    public bool supportsContentFilePath;
    public bool SupportsContentFilePath
    {
      get
      {
        return supportsContentFilePath;
      }
    }

    /// <summary>Path to file with received content.</summary>
    public string contentFilePath;
    public string ContentFilePath
    {
      get
      {
        if (supportsContentFilePath == false)
        {
          return null;
        }
        return contentFilePath;
      }
    }

    /// <summary>Number of redirects processed.</summary>
    public int redirectCount;
    public int RedirectCount
    {
      get
      {
        return redirectCount;
      }
    }

    /// <summary>Check whether progress is available.</summary>
    public bool IsProgressAvailable
    {
      get
      {
        return (expectedReceiveContentLength >= (long)0);
      }
    }

    /// <summary>Estimated total progress (0 to 1).</summary>
    public float Progress
    {
      get
      {
        if (expectedReceiveContentLength < (long)0)
        {
          return -1f;
        }

        ulong expectedLength = ExpectedAcquiredContentLength;
        ulong acquiredLength = AcquiredContentLength;

        if ((expectedLength == 0) ||
            (acquiredLength >= expectedLength))
        {
          return 1f;
        }

        return acquiredLength / (float)expectedLength;
      }
    }

    /// <summary>Estimated receive progress (0 to 1).</summary>
    public float ReceiveProgress
    {
      get
      {
        if (expectedReceiveContentLength < (long)0)
        {
          return -1f;
        }
        if ((expectedReceiveContentLength == (long)0) ||
            (receivedContentLength >= (ulong)expectedReceiveContentLength))
        {
          return 1f;
        }
        return receivedContentLength / (float)expectedReceiveContentLength;
      }
    }

    public HTTPResponse()
    {
      statusCode = (long)0;
      receivedContentLength = (ulong)0;
      resumedContentLength = (ulong)0;
      expectedReceiveContentLength = (long)-1;

      supportsContentStream = false;
      contentStream = null;
      supportsContentMemoryStream = false;
      supportsContentFilePath = false;
      contentFilePath = null;

      headers = null;
    }

    public byte[] ContentToBytes()
    {
      MemoryStream stream = ContentMemoryStream;

      if (stream == null)
      {
        return null;
      }

      return stream.ToArray();
    }

    public string ContentToString(Encoding enc = null)
    {
      byte[] bytes = ContentToBytes();

      if (bytes == null)
      {
        return null;
      }

      if (enc == null)
      {
        enc = Encoding.UTF8;
      }

      return enc.GetString(bytes);
    }

    public override string ToString()
    {
      return string.Format("HTTPResponse<StatusCode={0}," +
          "ReceivedContentLength={1},ResumedContentLength={2}," +
          "ExpectedReceiveContentLength={3}," +
          "AcquiredContentLength={4},ExpectedAcquiredContentLength={5}," +
          "ContentFilePath={6}," +
          "IsProgressAvailable={7},Progress={8:0.##}%," +
          "ReceiveProgress={9:0.##}%,RedirectCount={10},Headers={11}>",
          statusCode, receivedContentLength, resumedContentLength,
          ExpectedReceiveContentLength, AcquiredContentLength,
          ExpectedAcquiredContentLength, ContentFilePath,
          IsProgressAvailable,
          Progress * 100f, ReceiveProgress * 100, redirectCount, headers);
    }
  }
}
