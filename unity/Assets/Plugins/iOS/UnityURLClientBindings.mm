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

#import "UnityURLClient.h"
#import "UnityURLClientConnection.h"
#import "UnityURLClientConnectionManager.h"
#import <string.h>

#define STR_OR_EMPTY(__t__) (((__t__) == NULL) ? "" : (__t__))
#define STR_TO_NSSTRING(__t__) \
  [NSString stringWithUTF8String:STR_OR_EMPTY(__t__)]
#define STR_TO_NSSTRING_OR_NIL(__t__) \
  (((__t__) == NULL) ? nil : [NSString stringWithUTF8String:__t__])
#define NSSTRING_TO_STR(__t__) \
  (((__t__) == nil) ? NULL : strdup([(__t__) UTF8String]))

#define USE_CONNECTION(TRUE_EXPR)                               \
  UnityURLClientConnectionManager *instance;                    \
  UnityURLClientConnection *connection;                         \
                                                                \
  instance = [UnityURLClientConnectionManager sharedInstance];  \
  @synchronized(instance)                                       \
  {                                                             \
    connection = [instance connectionHavingID:connectionID];    \
    if (connection != nil)                                      \
    {                                                           \
      @synchronized(connection)                                 \
      {                                                         \
        TRUE_EXPR;                                              \
      }                                                         \
    }                                                           \
  }

extern "C"
{
  uint32_t _URLClientCreateHTTPConnection(const char *method, const char *url,
      int cachePolicy, float timeout);
  int _URLClientGetState(uint32_t connectionID);
  char *_URLClientGetErrorDomain(uint32_t connectionID);
  int64_t _URLClientGetErrorCode(uint32_t connectionID);
  char *_URLClientGetErrorDescription(uint32_t connectionID);
  void _URLClientSetAllowInvalidSSLCertificate(uint32_t connectionID,
      BOOL allow);
  void _URLClientSetAllowFollowRedirects(uint32_t connectionID,
      BOOL allow, int32_t maxCount);
  void _URLClientSetRequestHeader(uint32_t connectionID, const char *name,
      const char *value);
  void _URLClientSetRequestContent(uint32_t connectionID,
      unsigned char *src, uint64_t srcLength);
  void _URLClientSetRequestContentSource(uint32_t connectionID,
      const char *srcPath);
  void _URLClientSetRequestAuthCredential(uint32_t connectionID,
      const char *user, const char *password);
  void _URLClientAddAcceptableResponseStatusCodeRange(uint32_t connectionID,
      int64_t from, int64_t to);
  void _URLClientSetResponseContentDestination(uint32_t connectionID,
      const char *dstPath, BOOL allowResume);
  void _URLClientSendRequest(uint32_t connectionID);
  int64_t _URLClientGetResponseStatusCode(uint32_t connectionID);
  char *_URLClientGetResponseHeaderName(uint32_t connectionID, uint32_t index);
  char *_URLClientGetResponseHeaderValue(uint32_t connectionID, uint32_t index);
  int32_t _URLClientGetResponseRedirectCount(uint32_t connectionID);
  uint64_t _URLClientGetResponseContentLengthRead(uint32_t connectionID);
  uint64_t _URLClientGetResponseContentLengthResumed(uint32_t connectionID);
  int64_t _URLClientGetResponseContentExpectedLength(uint32_t connectionID);
  uint64_t _URLClientGetPendingResponseContentLength(uint32_t connectionID);
  BOOL _URLClientCheckAndResetResponseDirtyFlag(uint32_t connectionID);
  uint64_t _URLClientMovePendingResponseContent(uint32_t connectionID,
      unsigned char *dst, uint64_t dstLength);
  void _URLClientDestroyConnection(uint32_t connectionID);
}

uint32_t _URLClientCreateHTTPConnection(const char *method, const char *url,
    int cachePolicy, float timeout)
{
  UnityURLClientConnectionManager *instance;
  UnityURLClientConnection *connection;

  connection = [[UnityURLClientConnection alloc]
    initWithMethod:STR_TO_NSSTRING(method)
    url:STR_TO_NSSTRING(url)
    cachePolicy:(NSURLRequestCachePolicy)cachePolicy
    timeout:timeout];

  if (connection == nil)
  {
    return UNITY_CLIENT_URL_INVALID_CONNECTION_ID;
  }

  instance = [UnityURLClientConnectionManager sharedInstance];
  @synchronized(instance)
  {
    [instance queueConnection:connection];
  }

  return connection.connectionID;
}

int _URLClientGetState(uint32_t connectionID)
{
  UnityURLClientConnectionState state = UnknownState;

  USE_CONNECTION(
    state = [connection state];
  );

  return (int)state;
}

char *_URLClientGetErrorDomain(uint32_t connectionID)
{
  NSString *domain = kUnityURLClientDomain;

  USE_CONNECTION(
    NSError *error = [connection error];
    if (error == nil)
    {
      domain = nil;
    }
    else
    {
      domain = [error domain];

      if (domain == nil)
      {
        domain = @"";
      }
    }
  );

  return NSSTRING_TO_STR(domain);
}

int64_t _URLClientGetErrorCode(uint32_t connectionID)
{
  int64_t code = UnknownError;

  USE_CONNECTION(
    NSError *error = [connection error];
    if (error == nil)
    {
      code = NoneError;
    }
    else
    {
      code = (int64_t)[error code];
    }
  );

  return code;
}

char *_URLClientGetErrorDescription(uint32_t connectionID)
{
  NSString *description = @"Unknonwn";

  USE_CONNECTION(
    NSError *error = [connection error];
    if (error == nil)
    {
      description = nil;
    }
    else
    {
      NSString *domain = [error domain];

      if (((domain != nil) && [domain isEqualToString:kUnityURLClientDomain]))
      {
        description = UNITY_CLIENT_URL_ERROR_DESCRIPTION([error code]);
      }
      else
      {
        description = [error description];
      }
    }
  );

  return NSSTRING_TO_STR(description);
}

void _URLClientSetAllowInvalidSSLCertificate(uint32_t connectionID,
    BOOL allow)
{
  USE_CONNECTION(
    [connection setAllowInvalidSSLCertificate:allow];
  );
}

void _URLClientSetAllowFollowRedirects(uint32_t connectionID,
    BOOL allow, int32_t maxCount)
{
  USE_CONNECTION(
    [connection setAllowFollowRedirects:allow
                               maxCount:maxCount];
  );
}

void _URLClientSetRequestHeader(uint32_t connectionID, const char *name,
    const char *value)
{
  USE_CONNECTION(
    [connection setRequestHeader:STR_TO_NSSTRING(name)
                       withValue:STR_TO_NSSTRING(value)];
  );
}

void _URLClientSetRequestAuthCredential(uint32_t connectionID,
    const char *user, const char *password)
{
  USE_CONNECTION(
    [connection setCredentialFor:STR_TO_NSSTRING(user)
                   withPassword:STR_TO_NSSTRING(password)];
  );
}

void _URLClientSetRequestContent(uint32_t connectionID,
    unsigned char *src, uint64_t srcLength)
{
  NSData *content = [NSData dataWithBytes:src length:(NSUInteger)srcLength];

  USE_CONNECTION(
    [connection setRequestContentData:content];
  );
}

void _URLClientSetRequestContentSource(uint32_t connectionID,
    const char *srcPath)
{
  USE_CONNECTION(
    [connection setRequestContentSource:STR_TO_NSSTRING_OR_NIL(srcPath)];
  );
}

void _URLClientAddAcceptableResponseStatusCodeRange(uint32_t connectionID,
    int64_t from, int64_t to)
{
  StatusCodeRange range;

  range.from = from;
  range.to = to;

  NSValue *rangeValue =
    [NSValue valueWithBytes:&range objCType:@encode(StatusCodeRange)];

  if (rangeValue == nil)
  {
    return;
  }

  USE_CONNECTION(
      [connection addAcceptableStatusCodeRange:rangeValue];
  );
}

void _URLClientSetResponseContentDestination(uint32_t connectionID,
    const char *dstPath, BOOL allowResume)
{
  USE_CONNECTION(
    [connection setResponseContentDestination:STR_TO_NSSTRING_OR_NIL(dstPath)
                                  allowResume:allowResume];
  );
}

void _URLClientSendRequest(uint32_t connectionID)
{
  USE_CONNECTION(
    [connection sendRequest];
  );
}

int64_t _URLClientGetResponseStatusCode(uint32_t connectionID)
{
  int64_t statusCode = 0;

  USE_CONNECTION(
    statusCode = [connection statusCode];
  );

  return statusCode;
}

char *_URLClientGetResponseHeaderName(uint32_t connectionID, uint32_t index)
{
  NSString *name = nil;

  USE_CONNECTION(
    name = [connection responseHeaderNameWithIndex:(NSUInteger)index];
  );

  return NSSTRING_TO_STR(name);
}

char *_URLClientGetResponseHeaderValue(uint32_t connectionID, uint32_t index)
{
  NSString *value = nil;

  USE_CONNECTION(
    value = [connection responseHeaderValueWithIndex:(NSUInteger)index];
  );

  return NSSTRING_TO_STR(value);
}

int32_t _URLClientGetResponseRedirectCount(uint32_t connectionID)
{
  int count = (int64_t)-1;

  USE_CONNECTION(
    count = [connection responseRedirectCount];
  );

  return count;
}

uint64_t _URLClientGetResponseContentLengthRead(uint32_t connectionID)
{
  uint64_t length = (uint64_t)0;

  USE_CONNECTION(
    length = [connection responseContentLengthRead];
  );

  return length;
}

uint64_t _URLClientGetResponseContentLengthResumed(uint32_t connectionID)
{
  uint64_t length = (uint64_t)0;

  USE_CONNECTION(
    length = [connection responseContentLengthResumed];
  );

  return length;
}

int64_t _URLClientGetResponseContentExpectedLength(uint32_t connectionID)
{
  int64_t length = (int64_t)-1;

  USE_CONNECTION(
    length = [connection expectedResponseContentLength];
  );

  return length;
}

uint64_t _URLClientGetPendingResponseContentLength(uint32_t connectionID)
{
  uint64_t length = (uint64_t)0;

  USE_CONNECTION(
    length = [connection pendingResponseContentLength];
  );

  return length;
}

BOOL _URLClientCheckAndResetResponseDirtyFlag(uint32_t connectionID)
{
  BOOL isDirty = NO;

  USE_CONNECTION(
    isDirty = [connection checkAndResetResponseDirtyFlag];
  );

  return isDirty;
}

uint64_t _URLClientMovePendingResponseContent(uint32_t connectionID,
    unsigned char *dst, uint64_t dstLength)
{
  uint64_t length = (uint64_t)0;

  USE_CONNECTION(
    length = [connection movePendingResponseContentToBuffer:dst
                                            withCapacity:dstLength];
  );

  return length;
}

void _URLClientDestroyConnection(uint32_t connectionID)
{
  UnityURLClientConnectionManager *instance;
  UnityURLClientConnection *connection;

  instance = [UnityURLClientConnectionManager sharedInstance];
  @synchronized(instance)
  {
    connection = [instance dequeueConnection:connectionID];
    if (connection != nil)
    {
      @synchronized(connection)
      {
        [connection cancel];
      }
      [connection release];
    }
  }
}
