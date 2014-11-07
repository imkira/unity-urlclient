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

#define UNITY_CLIENT_URL_INVALID_CONNECTION_ID ((uint32_t)0)

typedef enum
{
  UnknownState = 0,
  InitializedState = 1,
  OpeningSourceFileState = 2,
  OpeningDestinationFileState = 3,
  SendingRequestState = 4,
  SentRequestState = 5,
  AuthenticatingState = 6,
  ReceivingDataState = 7,
  FinishedState = 8,
  CancelledState = 9
} UnityURLClientConnectionState;

typedef struct
{
  int64_t from;
  int64_t to;
} StatusCodeRange;

@interface UnityURLClientConnection : NSObject<NSURLConnectionDelegate, NSURLConnectionDataDelegate>
{
  uint32_t _connectionID;
  UnityURLClientConnectionState _state;
  NSError *_error;

  BOOL _isHTTP;
  NSURLConnection *_connection;
  NSMutableURLRequest *_request;
  NSData *_requestContent;
  NSString *_srcPath;
  BOOL _allowInvalidSSLCertificate;
  NSURLCredential *_credential;
  NSURLResponse *_response;
  BOOL _isResponseDirty;
  NSMutableArray *_acceptableStatusCodeRanges;
  NSString *_dstPath;
  NSFileHandle *_dstFile;
  int32_t _responseRedirectCount;
  int32_t _maxResponseRedirectCount;
  BOOL _allowFollowRedirect;
  uint64_t _responseContentLengthRead;
  uint64_t _responseContentLengthResumed;
  uint64_t _dstFileSize;
  BOOL _dstFileResume;
  NSMutableData *_responseContent;
  NSMutableData *_pendingResponseContent;
  uint64_t _pendingResponseContentOffset;
}

@property (nonatomic, assign) uint32_t connectionID;
@property (nonatomic, readonly) UnityURLClientConnectionState state;
@property (nonatomic, readonly) NSError *error;
@property (nonatomic, readonly) int32_t responseRedirectCount;
@property (nonatomic, readonly) uint64_t responseContentLengthRead;
@property (nonatomic, readonly) uint64_t responseContentLengthResumed;

- (id)initWithMethod:(NSString *)method
                 url:(NSString *)url
         cachePolicy:(NSURLRequestCachePolicy)cachePolicy
             timeout:(float)timeout;

- (void)setAllowInvalidSSLCertificate:(BOOL)allow;

- (void)setRequestHeader:(NSString *)name withValue:(NSString *)value;

- (void)addAcceptableStatusCodeRange:(NSValue *)range;

- (void)setCredentialFor:(NSString *)user withPassword:(NSString *)password;

- (void)setRequestContentData:(NSData *)data;

- (void)setRequestContentSource:(NSString *)srcPath;

- (void)setResponseContentDestination:(NSString *)dstPath allowResume:(BOOL)resume;

- (void)setAllowFollowRedirects:(BOOL)allow maxCount:(int32_t)count;

- (void)sendRequest;

- (void)cancel;

- (int64_t)statusCode;

- (NSString *)responseHeaderNameWithIndex:(NSUInteger)index;

- (NSString *)responseHeaderValueWithIndex:(NSUInteger)index;

- (int64_t)expectedResponseContentLength;

- (uint64_t)pendingResponseContentLength;

- (BOOL)checkAndResetResponseDirtyFlag;

- (uint64_t)movePendingResponseContentToBuffer:(unsigned char *)dst
                               withCapacity:(uint64_t)dstCapacity;
@end
