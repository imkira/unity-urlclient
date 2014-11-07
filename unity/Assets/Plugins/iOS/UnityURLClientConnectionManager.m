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

#import "UnityURLClientConnectionManager.h"

#define DEFAULT_QUEUE_CAPACITY 16

@implementation UnityURLClientConnectionManager
static UnityURLClientConnectionManager *sharedInstance;

+ (void)initialize
{
  static BOOL initialized = NO;

  if (!initialized)
  {
    initialized = YES;
    sharedInstance = [[UnityURLClientConnectionManager alloc]
      initWithCapacity:DEFAULT_QUEUE_CAPACITY];
  }
}

+ (UnityURLClientConnectionManager *)sharedInstance
{
  return sharedInstance;
}

- (id)initWithCapacity:(NSUInteger)numConnections
{
  self = [super init];

  if (self != nil)
  {
    _connections = [[NSMutableDictionary alloc] initWithCapacity:numConnections];
    _curConnectionId = UNITY_CLIENT_URL_INVALID_CONNECTION_ID;
  }

  return self;
}

- (void)dealloc
{
  [_connections release];
  [super dealloc];
}

- (UnityURLClientConnection *)connectionHavingID:(uint32_t)connectionID
{
  return [_connections objectForKey:[NSNumber numberWithUnsignedInt:connectionID]];
}

- (void)queueConnection:(UnityURLClientConnection *)connection
{
  NSNumber *connectionID;

  do
  {
    ++_curConnectionId;
    connectionID = [NSNumber numberWithUnsignedInt:_curConnectionId];
  }
  while ((_curConnectionId == UNITY_CLIENT_URL_INVALID_CONNECTION_ID) ||
      ([_connections objectForKey:connectionID] != nil));

  connection.connectionID = _curConnectionId;
  [_connections setObject:connection forKey:connectionID];
}

- (UnityURLClientConnection *)dequeueConnection:(uint32_t)connectionID
{
  NSNumber *_connectionID = [NSNumber numberWithUnsignedInt:connectionID];
  UnityURLClientConnection *existingConnection;

  existingConnection = [_connections objectForKey:_connectionID];
  if (existingConnection != nil)
  {
    [_connections removeObjectForKey:_connectionID];
  }

  return existingConnection;
}
@end
