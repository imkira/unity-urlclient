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

#import "UnityURLClientConnection.h"
#import <string.h>

#define DEFAULT_RESPONSE_CAPACITY (64 * 1024)

@interface UnityURLClientConnection ()
@property (nonatomic, readwrite, copy) NSError *error;
@property (nonatomic, readwrite, strong) NSURLConnection *connection;
@property (nonatomic, readwrite, strong) NSMutableURLRequest *request;
@property (nonatomic, readwrite, strong) NSData *requestContent;
@property (nonatomic, readwrite, strong) NSString *srcPath;
@property (nonatomic, readwrite, strong) NSURLCredential *credential;
@property (nonatomic, readwrite, strong) NSURLResponse *response;
@property (nonatomic, readwrite, strong) NSFileHandle *dstFile;
@property (nonatomic, readwrite, strong) NSString *dstPath;
@end

@implementation UnityURLClientConnection

@synthesize connectionID = _connectionID;
@synthesize state = _state;
@synthesize error = _error;
@synthesize connection = _connection;
@synthesize request = _request;
@synthesize requestContent = _requestContent;
@synthesize srcPath = _srcPath;
@synthesize credential = _credential;
@synthesize response = _response;
@synthesize responseRedirectCount = _responseRedirectCount;
@synthesize responseContentLengthRead = _responseContentLengthRead;
@synthesize responseContentLengthResumed = _responseContentLengthResumed;
@synthesize dstFile = _dstFile;
@synthesize dstPath = _dstPath;

- (id)initWithMethod:(NSString *)method
                 url:(NSString *)url
         cachePolicy:(NSURLRequestCachePolicy)cachePolicy
             timeout:(float)timeout
{
  self = [super init];

  if (self != nil)
  {
    _connectionID = UNITY_CLIENT_URL_INVALID_CONNECTION_ID;
    _state = InitializedState;
    _error = nil;
    _connection = nil;
    _requestContent = nil;
    _srcPath = nil;
    _allowInvalidSSLCertificate = NO;
    _credential = nil;
    _isHTTP = NO;
    _response = nil;
    _isResponseDirty = NO;
    _acceptableStatusCodeRanges = nil;
    _dstPath = nil;
    _dstFile = nil;
    _dstFileSize = (uint64_t)0;
    _dstFileResume = (BOOL)YES;
    _allowFollowRedirect = YES;
    _maxResponseRedirectCount = (int32_t)-1;
    _responseRedirectCount = (int32_t)0;
    _responseContent = nil;
    _responseContentLengthRead = (uint64_t)0;
    _responseContentLengthResumed = (uint64_t)0;
    _pendingResponseContent = nil;
    _pendingResponseContentOffset = (uint64_t)0;

    _request = [[NSMutableURLRequest alloc] init];
    [_request setTimeoutInterval:(NSTimeInterval)timeout];
    [_request setCachePolicy:cachePolicy];
    [_request setURL:[NSURL URLWithString:url]];
    [_request setHTTPMethod:method];
    [_request setHTTPShouldHandleCookies:NO];
  }

  return self;
}

- (void)dealloc
{
  [_pendingResponseContent release];
  [_responseContent release];
  if (_dstFile != nil)
  {
    [_dstFile closeFile];
    [_dstFile release];
  }
  [_dstPath release];
  [_acceptableStatusCodeRanges release];
  [_response release];
  [_credential release];
  [_srcPath release];
  [_requestContent release];
  [_connection release];
  [_request release];
  [_error release];
  [super dealloc];
}

- (BOOL)changeState:(UnityURLClientConnectionState)newState
          allowSame:(BOOL)allowSame
{
  if (_state <= UnknownState)
  {
    return NO;
  }

  if (_state == newState)
  {
    return allowSame;
  }

  if ((_state >= newState) || (_state >= FinishedState) || (_error != nil))
  {
    return NO;
  }

  _state = newState;
  return YES;
}

- (BOOL)setRequestHTTPBody
{
  uint64_t length;

  if (_srcPath != nil)
  {
    if (![self changeState:OpeningSourceFileState allowSame:NO])
    {
      return NO;
    }

    NSFileManager *fileManager = [NSFileManager defaultManager];
    NSDictionary *fileAttributes =
      [fileManager attributesOfItemAtPath:_srcPath error:nil];

    if (fileAttributes == nil)
    {
      self.error = UnityURLClientErrorWithCode(OpenSourceFileError);
      return NO;
    }

    NSInputStream *stream =
      [NSInputStream inputStreamWithFileAtPath:_srcPath];

    if (stream == nil)
    {
      self.error = UnityURLClientErrorWithCode(OpenSourceFileError);
      return NO;
    }

    NSNumber *fileSizeNumber = [fileAttributes objectForKey:NSFileSize];
    length = [fileSizeNumber unsignedLongLongValue];

    [_request setHTTPBodyStream:stream];
    self.srcPath = nil;
  }
  else if (_requestContent != nil)
  {
    length = [_requestContent length];
    [_request setHTTPBody:_requestContent];
    self.requestContent = nil;
  }
  else
  {
    return YES;
  }

  NSString *value = [NSString stringWithFormat:@"%llu", length];
  [_request setValue:value forHTTPHeaderField:@"Content-Length"];
  return YES;
}

- (BOOL)openDestinationFile
{
  if (_dstFile != nil)
  {
    return YES;
  }

  // open file handle
  self.dstFile = [NSFileHandle fileHandleForWritingAtPath:_dstPath];

  if (_dstFile == nil)
  {
    NSFileManager *fileManager = [NSFileManager defaultManager];

    // create empty file
    if (![fileManager createFileAtPath:_dstPath contents:nil attributes:nil])
    {
      self.error = UnityURLClientErrorWithCode(CreateDestinationFileError);
      return NO;
    }

    self.dstFile = [NSFileHandle fileHandleForWritingAtPath:_dstPath];
  }

  if (_dstFile == nil)
  {
    self.error = UnityURLClientErrorWithCode(OpenDestinationFileError);
    return NO;
  }

  _dstFileSize = [_dstFile seekToEndOfFile];
  return YES;
}

- (void)setResponseContentDestinationResumeHeader
{
  if (_dstFileSize > (uint64_t)0)
  {
    NSString *value = [NSString stringWithFormat:@"bytes=%llu-", _dstFileSize];
    [_request setValue:value forHTTPHeaderField:@"Range"];
  }
}

- (void)truncateDestinationFile
{
  _dstFileSize = (uint64_t)0;
  [_dstFile truncateFileAtOffset:_dstFileSize];
  [_dstFile synchronizeFile];
}

- (void)closeDestinationFile
{
  if (_dstFile != nil)
  {
    [_dstFile synchronizeFile];
    [_dstFile closeFile];
    self.dstFile = nil;
  }
}

- (void)deleteDestinationFile
{
  [self closeDestinationFile];

  if (_dstPath != nil)
  {
    [[NSFileManager defaultManager] removeItemAtPath:_dstPath error:nil];
  }
}

- (void)cancelWithError:(NSError *)error
{
  if (([self changeState:CancelledState allowSame:NO]) && (_connection != nil))
  {
    self.error = error;
    [_connection cancel];
    [self closeDestinationFile];
  }
}

+ (NSMutableData *)createDataForResponse:(NSURLResponse *)response
{
  NSUInteger capacity = (NSUInteger)DEFAULT_RESPONSE_CAPACITY;
  int64_t expectedContentLength = -1;

  if (response != nil)
  {
    expectedContentLength = [response expectedContentLength];
  }

  if ((expectedContentLength >= (int64_t)0) &&
      (expectedContentLength < (int64_t)capacity))
  {
    capacity = (NSUInteger)expectedContentLength;
  }

  return [[NSMutableData alloc] initWithCapacity:capacity];
}

- (NSHTTPURLResponse *)httpResponse
{
  if ((_response == nil) || (_isHTTP == NO))
  {
    return nil;
  }

  return (NSHTTPURLResponse *)_response;
}

- (BOOL)resumeDestination
{
  NSHTTPURLResponse *httpResponse = [self httpResponse];

  // http?
  if (httpResponse != nil)
  {
    NSInteger statusCode = [httpResponse statusCode];

    // invalid resume position specified?
    if (statusCode == 416)
    {
      [self deleteDestinationFile];
      self.error = UnityURLClientErrorWithCode(InvalidResumeOffsetError);
      return NO;
    }

    // resumable?
    if (statusCode == 206)
    {
      return YES;
    }
  }
  else
  {
    // unsupported (fallthrough)
  }

  [self truncateDestinationFile];
  return YES;
}

- (BOOL)isAcceptableStatusCode
{
  NSHTTPURLResponse *httpResponse = [self httpResponse];

  // not http?
  if (httpResponse == nil)
  {
    // unsupported for now
    return YES;
  }

  if (_acceptableStatusCodeRanges == nil)
  {
    return YES;
  }

  int64_t statusCode = [httpResponse statusCode];
  NSValue *rangeValue;
  StatusCodeRange range;

  for (rangeValue in _acceptableStatusCodeRanges)
  {
    [rangeValue getValue:&range];
    if ((range.from <= statusCode) && (range.to >= statusCode))
    {
      return YES;
    }
  }

  return NO;
}

- (void)translateKnownURLError:(NSError *)error
{
  switch ([error code])
  {
    case NSURLErrorCannotFindHost:
    case NSURLErrorDNSLookupFailed:
      error = UnityURLClientErrorWithCode(HostLookupError);
      break;

    case NSURLErrorCannotConnectToHost:
      error = UnityURLClientErrorWithCode(CannotConnectToHostError);
      break;

    case NSURLErrorNotConnectedToInternet:
      error = UnityURLClientErrorWithCode(CannotConnectToInternetError);
      break;

    case NSURLErrorNetworkConnectionLost:
      error = UnityURLClientErrorWithCode(ConnectionLostError);
      break;

    case NSURLErrorHTTPTooManyRedirects:
      error = UnityURLClientErrorWithCode(TooManyRedirectsError);
      break;

    case NSURLErrorTimedOut:
      error = UnityURLClientErrorWithCode(ConnectionTimeoutError);
      break;

    default:
      break;
  }
  self.error = error;
}

#pragma mark - NSURLConnectionDelegate methods

- (void)connection:(NSURLConnection *)connection didFailWithError:(NSError *)error
{
  @synchronized(self)
  {
    [self closeDestinationFile];
    if (_error == nil)
    {
      if ([[error domain] isEqualToString:NSURLErrorDomain])
      {
        [self translateKnownURLError:error];
      }
      else
      {
        self.error = error;
      }
    }
    self.connection = nil;
  }
}

- (BOOL)connectionShouldUseCredentialStorage:(NSURLConnection *)connection
{
  return YES;
}

- (BOOL)validateServerTrust:(SecTrustRef)serverTrust
{
  SecTrustResultType result = kSecTrustResultInvalid;
  OSStatus status = SecTrustEvaluate(serverTrust, &result);

  if (status != errSecSuccess)
  {
    return NO;
  }

  if ((result == kSecTrustResultUnspecified) ||
      (result == kSecTrustResultProceed) ||
      ((result == kSecTrustResultConfirm) && (_allowInvalidSSLCertificate)))
  {
    return YES;
  }

  return NO;
}

- (BOOL)canAuthenticate:(NSURLProtectionSpace *)protectionSpace
         ignoreSSLCheck:(BOOL)ignoreSSLCheck
{
  NSString *method = protectionSpace.authenticationMethod;

  if ([method isEqualToString:NSURLAuthenticationMethodClientCertificate])
  {
    return NO;
  }

  if ([method isEqualToString:NSURLAuthenticationMethodServerTrust])
  {
    if (ignoreSSLCheck)
    {
      return _allowInvalidSSLCertificate;
    }

    if ([self validateServerTrust:protectionSpace.serverTrust])
    {
      return YES;
    }

    return _allowInvalidSSLCertificate;
  }

  return YES;
}

- (BOOL)connection:(NSURLConnection *)connection
canAuthenticateAgainstProtectionSpace:(NSURLProtectionSpace *)protectionSpace
{
  @synchronized(self)
  {
    [self changeState:AuthenticatingState allowSame:YES];
    return [self canAuthenticate:protectionSpace
                  ignoreSSLCheck:YES];
  }
}

- (void)handleAuthentication:(NSURLAuthenticationChallenge *)challenge
                    allowSSL:(BOOL)allowSSL
{
  NSString *method = challenge.protectionSpace.authenticationMethod;
  NSURLCredential *credential = nil;

  if ([method isEqualToString:NSURLAuthenticationMethodServerTrust])
  {
    if (allowSSL == YES)
    {
      SecTrustRef serverTrust = challenge.protectionSpace.serverTrust;
      credential = [NSURLCredential credentialForTrust:serverTrust];
    }
  }
  else if ((_credential != nil) && ([challenge previousFailureCount] == 0))
  {
    credential = _credential;
  }

  if (credential != nil)
  {
    [challenge.sender useCredential:credential
         forAuthenticationChallenge:challenge];
  }
  else
  {
    [challenge.sender
      continueWithoutCredentialForAuthenticationChallenge:challenge];
  }
}

- (void)connection:(NSURLConnection *)connection
didReceiveAuthenticationChallenge:(NSURLAuthenticationChallenge *)challenge
{
  @synchronized(self)
  {
    [self changeState:AuthenticatingState allowSame:YES];
    [self handleAuthentication:challenge
                      allowSSL:_allowInvalidSSLCertificate];
  }
}

#pragma mark - NSURLConnectionDataDelegate methods

- (void)connection:(NSURLConnection *)connection
willSendRequestForAuthenticationChallenge:(NSURLAuthenticationChallenge *)challenge
{
  @synchronized(self)
  {
    [self changeState:AuthenticatingState allowSame:YES];
    if ([self canAuthenticate:challenge.protectionSpace
               ignoreSSLCheck:NO])
    {
      [self handleAuthentication:challenge allowSSL:YES];
    }
    else
    {
      [challenge.sender cancelAuthenticationChallenge:challenge];
    }
  }
}

- (NSURLRequest *)connection:(NSURLConnection *)connection
             willSendRequest:(NSURLRequest *)request
            redirectResponse:(NSURLResponse *)response
{
  @synchronized(self)
  {
    if (response != nil)
    {
      int32_t count = _responseRedirectCount + 1;

      // sanity check
      if (count > _responseRedirectCount)
      {
        _responseRedirectCount = count;
      }


      if (_allowFollowRedirect != YES)
      {
        return nil;
      }

      if ((_maxResponseRedirectCount > (int32_t)0) &&
          (_responseRedirectCount > _maxResponseRedirectCount))
      {
        [self cancelWithError:UnityURLClientErrorWithCode(TooManyRedirectsError)];
        return nil;
      }

      NSMutableURLRequest *newRequest = [[request mutableCopy] autorelease];
      [newRequest setURL: [request URL]];
      // TODO: should not only set the new URL but also cookies that may have
      // been received in the request
      request = newRequest;
    }

    return request;
  }
}

- (void)connection:(NSURLConnection *)connection
didReceiveResponse:(NSURLResponse *)response
{
  @synchronized(self)
  {
    if (![self changeState:ReceivingDataState allowSame:YES])
    {
      return;
    }

    if (_response != nil)
    {
      _isResponseDirty = YES;
    }

    _isHTTP = [response isKindOfClass:[NSHTTPURLResponse class]];
    self.response = response;

    // TODO: currently, only HTTP is supported
    if (_isHTTP == NO)
    {
      [self cancelWithError:UnityURLClientErrorWithCode(UnsupportedProtocolError)];
      return;
    }

    _responseContentLengthResumed = (uint64_t)0;
    _responseContentLengthRead = (uint64_t)0;

    if ([self isAcceptableStatusCode] != YES)
    {
      [self cancelWithError:UnityURLClientErrorWithCode(UnacceptableStatusCodeError)];
      return;
    }

    if (_dstFile != nil)
    {
      if (_dstFileSize > (uint64_t)0)
      {
        if (_dstFileResume == YES)
        {
          if ([self resumeDestination] != YES)
          {
            [self cancel];
            return;
          }
          _responseContentLengthResumed = _dstFileSize;
        }
        else
        {
          [self truncateDestinationFile];
        }
      }
    }
    else
    {
      if (_responseContent != nil)
      {
        [_responseContent setLength:0];
      }

      if (_pendingResponseContent != nil)
      {
        [_pendingResponseContent setLength:0];
      }

      _pendingResponseContentOffset = (uint64_t)0;
    }
  }
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data
{
  uint64_t length = (uint64_t)[data length];

  @synchronized(self)
  {
    if (![self changeState:ReceivingDataState allowSame:YES])
    {
      return;
    }

    if (_dstFile != nil)
    {
      [_dstFile writeData:data];
    }
    else
    {
      if (_responseContent == nil)
      {
        _responseContent =
          [UnityURLClientConnection createDataForResponse:_response];
      }

      if (_responseContent == nil)
      {
        [self cancelWithError:UnityURLClientErrorWithCode(AllocationError)];
        return;
      }

      [_responseContent appendData:data];
    }

    _responseContentLengthRead += length;
  }
}

- (void)connection:(NSURLConnection *)connection
   didSendBodyData:(NSInteger)bytesWritten
 totalBytesWritten:(NSInteger)totalBytesWritten
 totalBytesExpectedToWrite:(NSInteger)totalBytesExpectedToWrite
{
}

- (NSCachedURLResponse *)connection:(NSURLConnection *)connection
                  willCacheResponse:(NSCachedURLResponse *)cachedResponse
{
  return cachedResponse;
}

- (void)connectionDidFinishLoading:(NSURLConnection *)connection
{
  @synchronized(self)
  {
    [self closeDestinationFile];
    [self changeState:FinishedState allowSame:NO];
    self.connection = nil;
  }
}

#pragma - mark Integration with bindings

- (void)setAllowInvalidSSLCertificate:(BOOL)allow
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    _allowInvalidSSLCertificate = allow;
  }
}

- (void)setAllowFollowRedirects:(BOOL)allow maxCount:(int32_t)count
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    _allowFollowRedirect = allow;
    _maxResponseRedirectCount = count;
  }
}

- (void)setRequestHeader:(NSString *)name withValue:(NSString *)value
{
  if ((_request != nil) &&
      (([self changeState:InitializedState allowSame:YES]) == YES))
  {
    [_request setValue:value forHTTPHeaderField:name];
  }
}

- (void)addAcceptableStatusCodeRange:(NSValue *)range
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    if (_acceptableStatusCodeRanges == nil)
    {
      _acceptableStatusCodeRanges = [[NSMutableArray alloc] init];

      if (_acceptableStatusCodeRanges == nil)
      {
        self.error = UnityURLClientErrorWithCode(AllocationError);
        return;
      }
    }
    [_acceptableStatusCodeRanges addObject:range];
  }
}

- (void)setCredentialFor:(NSString *)user withPassword:(NSString *)password
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    self.credential =
      [NSURLCredential credentialWithUser:user
                                 password:password
                              persistence:NSURLCredentialPersistenceNone];
  }
}

- (void)setRequestContentData:(NSData *)data
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    self.requestContent = data;
    self.srcPath = nil;
  }
}

- (void)setRequestContentSource:(NSString *)srcPath
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    self.srcPath = srcPath;
    self.requestContent = nil;
  }
}

- (void)setResponseContentDestination:(NSString *)dstPath allowResume:(BOOL)resume
{
  if (([self changeState:InitializedState allowSame:YES]) == YES)
  {
    self.dstPath = dstPath;
    _dstFileResume = resume;
  }
}

- (int64_t)statusCode
{
  NSHTTPURLResponse *response = [self httpResponse];

  if (response != nil)
  {
    return [response statusCode];
  }

  return (int64_t)-1;
}

- (NSString *)responseHeaderNameWithIndex:(NSUInteger)index
{
  NSString *name = nil;
  NSHTTPURLResponse *response = [self httpResponse];

  if (response != nil)
  {
    NSArray *headers = [[response allHeaderFields] allKeys];
    if (index < [headers count])
    {
      name = [headers objectAtIndex:index];
    }
  }

  return name;
}

- (NSString *)responseHeaderValueWithIndex:(NSUInteger)index
{
  NSString *value = nil;
  NSHTTPURLResponse *response = [self httpResponse];

  if (response != nil)
  {
    NSArray *headers = [[response allHeaderFields] allValues];
    if (index < [headers count])
    {
      value = [headers objectAtIndex:index];
    }
  }

  return value;
}

- (int64_t)expectedResponseContentLength
{
  if (_response != nil)
  {
    return [_response expectedContentLength];
  }

  return (int64_t)-1;
}

- (uint64_t)pendingResponseContentLength
{
  uint64_t length = (uint64_t)0;

  if (_pendingResponseContent != nil)
  {
    length = [_pendingResponseContent length] - _pendingResponseContentOffset;
  }

  if (_responseContent != nil)
  {
    length += [_responseContent length];
  }

  return length;
}

- (uint64_t)copyResponseContent:(NSData *)srcData
                  fromOffset:(uint64_t)srcOffset
                    toBuffer:(unsigned char *)dst
                withCapacity:(uint64_t)dstCapacity
{
  const unsigned char *src;

  if ((dst == NULL) || (dstCapacity <= 0) ||
      (srcData == nil) ||
      ((src = (const unsigned char *)[srcData bytes]) == NULL))
  {
    return (uint64_t)0;
  }

  uint64_t length = (uint64_t)[srcData length];

  if ((length <= (uint64_t)0) || (srcOffset >= length))
  {
    return (uint64_t)0;
  }

  length = length - srcOffset;
  length = MIN(length, dstCapacity);
  memcpy(dst, src + srcOffset, (size_t)length);

  return length;
}

- (BOOL)checkAndResetResponseDirtyFlag
{
  if (_isResponseDirty != NO)
  {
    _isResponseDirty = NO;
    return YES;
  }

  return NO;
}

- (uint64_t)movePendingResponseContentToBuffer:(unsigned char *)dst
                                  withCapacity:(uint64_t)dstCapacity
{
  uint64_t copied;

  if (_isResponseDirty != NO)
  {
    return (uint64_t)0;
  }

  // copy from pending data buffer
  if ((_pendingResponseContent != nil) &&
      ((uint64_t)[_pendingResponseContent length] > (uint64_t)0))
  {
    copied = [self copyResponseContent:_pendingResponseContent
                         fromOffset:_pendingResponseContentOffset
                           toBuffer:dst
                       withCapacity:dstCapacity];

    // no data copied?
    if (copied <= (uint64_t)0)
    {
      return copied;
    }

    _pendingResponseContentOffset += copied;

    // all data copied?
    if (_pendingResponseContentOffset >=
        (uint64_t)[_pendingResponseContent length])
    {
      [_pendingResponseContent setLength:0];
      _pendingResponseContentOffset = (uint64_t)0;
    }

    return copied;
  }

  // copy from main data buffer
  if ((_responseContent != nil) &&
      ((uint64_t)[_responseContent length] > (uint64_t)0))
  {
    copied = [self copyResponseContent:_responseContent
                         fromOffset:(uint64_t)0
                           toBuffer:dst
                       withCapacity:dstCapacity];

    // no data copied?
    if (copied <= (uint64_t)0)
    {
      return copied;
    }

    // all data copied?
    if (copied >= (uint64_t)[_responseContent length])
    {
      [_responseContent setLength:0];
      return copied;
    }

    if (_pendingResponseContent == nil)
    {
      _pendingResponseContent =
        [UnityURLClientConnection createDataForResponse:_response];
    }
    else
    {
      [_pendingResponseContent setLength:0];
    }

    // switch pending and main data buffers
    NSMutableData *responseContent = _responseContent;
    _responseContent = _pendingResponseContent;
    _pendingResponseContent = responseContent;
    _pendingResponseContentOffset = copied;
    return copied;
  }

  return (uint64_t)0;
}

- (void)sendRequest
{
  if ([self setRequestHTTPBody] != YES)
  {
    return;
  }

  if (_dstPath != nil)
  {
    if (![self changeState:OpeningDestinationFileState allowSame:NO])
    {
      return;
    }

    if ([self openDestinationFile] != YES)
    {
      return;
    }
  }

  if (![self changeState:SendingRequestState allowSame:NO])
  {
    return;
  }

  if (_dstFile != nil)
  {
    if (_dstFileResume == YES)
    {
      [self setResponseContentDestinationResumeHeader];
    }
  }

  self.connection = [[NSURLConnection alloc] initWithRequest:_request
                                                    delegate:self];

  self.request = nil;

  if (_connection == nil)
  {
    self.error = UnityURLClientErrorWithCode(InitConnectionError);
    return;
  }

  [self changeState:SentRequestState allowSame:NO];
}

- (void)cancel
{
  [self cancelWithError:_error];
}
@end
