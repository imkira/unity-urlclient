# unity-urlclient

unity-urlclient is a [Unity 3D](http://unity3d.com) plugin for downloading
files via HTTP/HTTPS.

## Requirements

* Unity 3.4.x Pro and above.

## Features

* Multi-platform: iOS/Android/MacOSX support.
* Protocol: HTTP/HTTPS
* Receiving Data: Stream, MemoryStream, byte array, low-overhead direct download and storage (resume support).
* Sending Data: byte array, low-overhead direct upload support.
* Settings: HTTP Header, Query String, Cookie, Post Body
* Advanced: Progress, Cache, Redirect, Timeout

# Roadmap

* Upload with Stream support
* Cookie Storage

## Download & Installation

Latest version: v1.0.0

### Unity 3.x

* [Download Plugin](http://dl.bintray.com/imkira/unity-urlclient/unity3x-urlclient-1.0.0.unitypackage)
* [Download Demo](http://dl.bintray.com/imkira/unity-urlclient/unity3x-urlclient-demo.unitypackage)

### Unity 4.x

* [Download Plugin](http://dl.bintray.com/imkira/unity-urlclient/unity4x-urlclient-1.0.0.unitypackage)
* [Download Demo](http://dl.bintray.com/imkira/unity-urlclient/unity4x-urlclient-demo.unitypackage)

## How To Use

### Simple Request

```csharp
using (URLClient.HTTPClient client = new URLClient.HTTPClient("unity3d.com"))
{
  yield return StartCoroutine(client.WaitUntilDone());

  ///
  // check response
  // ...
}
```

### Advanced Request

You can create a request object with:

```csharp
URLClient.HTTPRequest request = new URLClient.HTTPRequest();
```

Then you customize the request like you want like ([check API here](http://github.com/imkira/unity-urlclient/blob/master/unity/Assets/Plugins/URLClient/http/HTTPRequest.cs))

```csharp
request.SetURL("http://www.google.com").
  AppendQueryParameter("q", "unity").
  SetHeader("User-Agent", "dummy user agent").
  AppendCookie("name", "value").
  Get();
```

Then you create a URLClient with:

```csharp
using (URLClient.HTTPClient client = new URLClient.HTTPClient(request))
{
  yield return StartCoroutine(client.WaitUntilDone());

  ///
  // check response
  // ...
}
```

### Checking Response

You can check the response any time during or after you create the client.
It may return ```null``` in case there is no response.

You can get the response with:

```csharp
if (client.Response == null)
{
  // no response yet
}
else
{
  // commonly used fields:
  // client.Response.StatusCode;
  // client.Response.ContentToBytes();
  // client.Response.ContentToString();
  // client.Response.Progress;
  // etc.
}
```

```client.Response``` follows the ```URLClient.IHTTPResponse``` interface:

```csharp
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
```

### Error Handling

You can check for error at any time during or after you create the client.

```csharp
if (client.Error != null)
{
  // client.Error.Domain
  // client.Error.Code
  // client.Error.Description
}
```

### Cancelling Client

You can cancel a running client at any time with:

```csharp
client.Cancel();
```

### Coroutines

You don't have to use coroutines. You can create and run a client with:

```csharp
URLClient.HTTPClient client = new URLClient.HTTPClient(...);
```

Then, instead of ```yield return StartCoroutine(client.WaitUntilDone());``` you
can call the following at any point in time:

```csharp
if (client.IsDone)
{
  // client finished!
  // check client.Response
}
else
{
  if (client.Response == null)
  {
    // no response yet
  }
  else
  {
    // check progress with client.Response.Progress
    // etc.
  }
}
```

However, you must destroy the client when you don't need it (otherwise you will
get memory leaks):

```
client.Dispose();
client = null;
```

## Demonstration

[unity/Assets/URLClient/Demo/URLClientDemo.cs](http://github.com/imkira/unity-urlclient/blob/master/unity/Assets/URLClient/Demo/URLClientDemo.cs)

## Contribute

* Found a bug?
* Want to contribute and add a new feature?

Please fork this project and send me a pull request!

## License

unity-urlclient is licensed under the MIT license:

www.opensource.org/licenses/MIT

## Copyright

Copyright (c) 2013 Mario Freitas. See
[LICENSE.txt](http://github.com/imkira/unity-urlclient/blob/master/LICENSE.txt)
for further details.
