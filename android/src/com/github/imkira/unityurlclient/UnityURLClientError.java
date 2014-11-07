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

package com.github.imkira.unityurlclient;

public class UnityURLClientError {

    public enum Error {
        NoneError,
        UnknownError,
        AllocationError,
        InitConnectionError,
        UnsupportedProtocolError,
        TooManyRedirectsError,
        UnacceptableStatusCodeError,
        InvalidResumeOffsetError,
        OpenSourceFileError,
        OpenDestinationFileError,
        CreateDestinationFileError,
        HostLookupError,
        CannotConnectToHostError,
        CannotConnectToInternetError,
        ConnectionLostError,
        ConnectionTimeoutError,
        LastError,
    }

    public static final String kUnityURLClientNoneError = null;
    public static final String kUnityURLClientUnknownError = "Unknown error";
    public static final String kUnityURLClientAllocationError = "Could not allocate resource/memory";
    public static final String kUnityURLClientInitConnectionError = "Could not initialize connection";
    public static final String kUnityURLClientUnsupportedProtocolError = "Unsupported protocol detected";
    public static final String kUnityURLClientTooManyRedirectsError = "Too many redirects";
    public static final String kUnityURLClientUnacceptableStatusCodeError = "Unacceptable status code error received.";
    public static final String kUnityURLClientInvalidResumeOffsetError = "Attempt to use invalid resume offset";
    public static final String kUnityURLClientOpenSourceFileError = "Could not open source file for reading";
    public static final String kUnityURLClientOpenDestinationFileError = "Could not open destination file for writing";
    public static final String kUnityURLClientCreateDestinationFileError = "Could not create destination file";
    public static final String kUnityURLClientHostLookupError = "Could not lookup hostname";
    public static final String kUnityURLClientCannotConnectToHostError = "Cannot connect to host";
    public static final String kUnityURLClientCannotConnectToInternetError = "Cannot connect to internet";
    public static final String kUnityURLClientConnectionLostError = "Connection to host was lost";
    public static final String kUnityURLClientConnectionTimeoutError = "Connection timed out";

    public static final String[] ErrorDescriptions = new String[] {
        kUnityURLClientNoneError,
        kUnityURLClientUnknownError,
        kUnityURLClientAllocationError,
        kUnityURLClientInitConnectionError,
        kUnityURLClientUnsupportedProtocolError,
        kUnityURLClientTooManyRedirectsError,
        kUnityURLClientUnacceptableStatusCodeError,
        kUnityURLClientInvalidResumeOffsetError,
        kUnityURLClientOpenSourceFileError,
        kUnityURLClientOpenDestinationFileError,
        kUnityURLClientCreateDestinationFileError,
        kUnityURLClientHostLookupError,
        kUnityURLClientCannotConnectToHostError,
        kUnityURLClientCannotConnectToInternetError,
        kUnityURLClientConnectionLostError,
        kUnityURLClientConnectionTimeoutError
    };

    public static String getErrorDescriptions(int code) {
        if (code >= 0 && code < Error.LastError.ordinal()) {
            return ErrorDescriptions[code];
        }

        return null;
    }
}
