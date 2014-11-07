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

#define kUnityURLClientDomain @"UnityURLClient"

typedef enum
{
#define kUnityURLClientNoneError nil
  NoneError = 0,

#define kUnityURLClientUnknownError @"Unknown error"
  UnknownError = 1,

#define kUnityURLClientAllocationError @"Could not allocate resource/memory"
  AllocationError = 2,

#define kUnityURLClientInitConnectionError @"Could not initialize connection"
  InitConnectionError = 3,

#define kUnityURLClientUnsupportedProtocolError @"Unsupported protocol detected"
  UnsupportedProtocolError = 4,

#define kUnityURLClientTooManyRedirectsError @"Too many redirects"
  TooManyRedirectsError = 5,

#define kUnityURLClientUnacceptableStatusCodeError @"Unacceptable status code error received."
  UnacceptableStatusCodeError = 6,

#define kUnityURLClientInvalidResumeOffsetError @"Attempt to use invalid resume offset"
  InvalidResumeOffsetError = 7,

#define kUnityURLClientOpenSourceFileError @"Could not open source file for reading"
  OpenSourceFileError = 8,

#define kUnityURLClientOpenDestinationFileError @"Could not open destination file for writing"
  OpenDestinationFileError = 9,

#define kUnityURLClientCreateDestinationFileError @"Could not create destination file"
  CreateDestinationFileError = 10,

#define kUnityURLClientHostLookupError @"Could not lookup hostname"
  HostLookupError = 11,

#define kUnityURLClientCannotConnectToHostError @"Cannot connect to host"
  CannotConnectToHostError = 12,

#define kUnityURLClientCannotConnectToInternetError @"Cannot connect to internet"
  CannotConnectToInternetError = 13,

#define kUnityURLClientConnectionLostError @"Connection to host was lost"
  ConnectionLostError = 14,

#define kUnityURLClientConnectionTimeoutError @"Connection timed out"
  ConnectionTimeoutError = 15,

  // unused
  LastError
} UnityURLClientError;

extern NSString* UnityURLClientErrorDescriptions[];

#define UNITY_CLIENT_URL_ERROR_DESCRIPTION(__code__)   \
  ((((int64_t)(__code__) >= (int64_t)0) &&             \
   ((int64_t)(__code__) < (int64_t)LastError)) ?       \
    UnityURLClientErrorDescriptions[__code__] : nil)

#define UnityURLClientErrorWithCode(__code__) \
  [NSError errorWithDomain:kUnityURLClientDomain code:(__code__) userInfo:nil]
