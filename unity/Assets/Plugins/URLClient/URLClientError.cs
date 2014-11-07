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
  public class Error
  {
    public enum KnownCode
    {
      // basic
      BasicErrorMin = 0,
      Unknown = 1,
      Allocation = 2,
      InitConnection = 3,
      UnsupportedProtocol = 4,
      TooManyRedirects = 5,
      UnacceptableStatusCode = 6,
      InvalidResumeOffset = 7,
      OpenSourceFile = 8,
      OpenDestinationFile = 9,
      CreateDestinationFile = 10,
      HostLookupError = 11,
      CannotConnectToHostError = 12,
      CannotConnectToInternetError = 13,
      ConnectionLostError = 14,
      ConnectionTimeoutError = 15,
      BasicErrorMax = 16,

      // extended
      ExtendedErrorMin = 100,
      ClientAlreadyDisposedError = 101,
      RequestNotSetError = 102,
      RequestURLInvalid = 103,
      RequestHandlerDidNotStart = 104,
      ResponseHandlerDidNotStart = 105,
      ResponseHandlingError = 106,
      ExtendedErrorMax = 102
    };

    protected string _domain;

    /// <summary>Error domain.</summary>
    public string Domain
    {
      get
      {
        return _domain;
      }
    }

    protected long _code;

    /// <summary>Error code.</summary>
    public long Code
    {
      get
      {
        return _code;
      }
    }

    protected string _description;

    /// <summary>Error description.</summary>
    public string Description
    {
      get
      {
        return _description;
      }
    }

    /// <summary>Whether this is a known error or not.</summary>
    public bool IsKnown
    {
      get
      {
        return ((_domain == Bindings.ERROR_DOMAIN) &&
            ((_code > (long)KnownCode.BasicErrorMin) &&
             (_code < (long)KnownCode.BasicErrorMax)) ||
            ((_code > (long)KnownCode.ExtendedErrorMin) &&
             (_code < (long)KnownCode.ExtendedErrorMax)));
      }
    }

    public Error(string domain, long code, string description)
    {
      _domain = domain;
      _code = code;
      _description = description;
    }

    public bool IsKnownCode(KnownCode code)
    {
      return ((_domain == Bindings.ERROR_DOMAIN) && ((long)code == _code));
    }

    public override string ToString()
    {
      return string.Format("Error<Domain={0},Code={1},Description={2}," +
          "IsKnown={3}>", _domain, _code, _description, IsKnown);
    }
  }
}
