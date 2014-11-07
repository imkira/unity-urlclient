using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections;

// add the following to avoid having to prepend everything with "URLClient."
using URLClient;

public class URLClientDemo : MonoBehaviour
{
  HTTPClient _client;

  string _test = "";
  string _log = "";
  int _curIndex;
  int _demoIndex;
  delegate IEnumerator DemoFunction();
  DemoFunction[] _demos;
  float _initialTime;

  void Awake()
  {
    _demos = new DemoFunction[]
    {
      DemoSimple,
      DemoFormPost,
      DemoFilePost,
      DemoQueryHeaderAndCookies,
      DemoAcceptableStatusCodes,
      DemoHTTPS,
      DemoAcceptInvalidHTTPSCertificates,
      DemoBasicAuthClient,
      DemoRedirectControl,
      DemoTimeoutControl,
      DemoCacheControl,
      DemoErrorHandling,
      DemoBigFileIntegrityTest,
      DemoBigFileDownload
    };

    Next();
    _demoIndex = 0;
  }

  IEnumerator DemoSimple()
  {
    _test = "DemoSimple";

    using (_client = new HTTPClient("unity3d.com"))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoFormPost()
  {
    _test = "DemoFormPost";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.hashemian.com/tools/form-post-tester.php/test123").
      SetContentString("foo=bar&cafe=babe", "application/octet-stream").
      Post();

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoFilePost()
  {
    _test = "DemoFilePost";

    string srcPath = Application.temporaryCachePath;
    srcPath = System.IO.Path.Combine(srcPath, "data");
    byte[] data = Encoding.UTF8.GetBytes("hello\nworld\n!!!");

    using (FileStream stream = new FileStream(srcPath, FileMode.Create,
          FileAccess.Write, FileShare.ReadWrite))
    {
      stream.Write(data, 0, data.Length);
    }

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.hashemian.com/tools/form-post-tester.php/test123").
      SetContentFromPath(srcPath, "application/octet-stream").
      Post();

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoQueryHeaderAndCookies()
  {
    _test = "DemoQueryHeaderAndCookies";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.google.com").
      AppendQueryParameter("q", "unity").
      SetHeader("User-Agent", "dummy user agent").
      AppendCookie("name", "value").
      Get();

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoAcceptableStatusCodes()
  {
    _test = "DemoAcceptableStatusCodes";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://google.com/nonexistent").
      AppendQueryParameter("q", "unity");

    HTTPResponseMemoryStreamHandler responseHandler =
      new HTTPResponseMemoryStreamHandler();

    responseHandler.AddAcceptableStatusCodeRange(200, 299);

    using (_client = new HTTPClient(request, responseHandler))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoHTTPS()
  {
    _test = "DemoHTTPS";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("https://google.com").
      AppendQueryParameter("q", "unity");

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoAcceptInvalidHTTPSCertificates()
  {
    _test = "DemoAcceptInvalidHTTPSCertificates";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("https://google.com").
      AppendQueryParameter("q", "unity");

    HTTPResponseMemoryStreamHandler responseHandler =
      new HTTPResponseMemoryStreamHandler();

    responseHandler.SetAllowInvalidSSLCertificates(true);

    using (_client = new HTTPClient(request, responseHandler))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoBasicAuthClient()
  {
    _test = "DemoBasicAuthClient";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://browserspy.dk/password-ok.php").
      SetAuthCredential("test", "test");

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoRedirectControl()
  {
    _test = "DemoRedirectControl";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("https://google.com").
      AppendQueryParameter("q", "unity");

    HTTPResponseMemoryStreamHandler responseHandler =
      new HTTPResponseMemoryStreamHandler();

    responseHandler.SetAllowFollowRedirects(true);
    responseHandler.SetMaxRedirectCount(10);

    using (_client = new HTTPClient(request, responseHandler))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoTimeoutControl()
  {
    _test = "DemoTimeoutControl";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.wikipedia.com");

    using (_client = new HTTPClient(request))
    {
      _client.ConnectionTimeout = 0.01f;
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);

      Error err = _client.Error;
      if (err != null)
      {
        _log += "\n\nIs Timeout Error? " +
          (err.IsKnownCode(Error.KnownCode.ConnectionTimeoutError) ? "YES" : "NO");
      }
    }
  }

  IEnumerator DemoCacheControl()
  {
    _test = "DemoCacheControl";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.google.com").
      AppendQueryParameter("q", "unity");

    HTTPResponseMemoryStreamHandler responseHandler =
      new HTTPResponseMemoryStreamHandler();

    responseHandler.SetCachePolicy(CachePolicy.ReloadIgnoringLocalAndRemoteCacheData);

    using (_client = new HTTPClient(request, responseHandler))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);
    }
  }

  IEnumerator DemoErrorHandling()
  {
    _test = "DemoErrorHandling";

    HTTPRequest request = new HTTPRequest();

    request.SetURL("http://www.wikipeeeeeedia.com");

    using (_client = new HTTPClient(request))
    {
      yield return StartCoroutine(_client.WaitUntilDone());
      DebugAll(_client);

      Error err = _client.Error;
      if (err != null)
      {
        _log += "\n\nIs Host Lookup Error? " +
          (err.IsKnownCode(Error.KnownCode.HostLookupError) ? "YES" : "NO");
      }
    }
  }

  IEnumerator DemoBigFileIntegrityTest()
  {
    _test = "DemoBigFileIntegrityTest";

    // By default, Unity-URLClient loads data into memory, and therefore
    // loading a big file into memory like this, is NOT recommended!
    // In order to reduce big data buffer exchange between native and unity side,
    // and in order to have a constantly updated buffer in unity side, Unity-URLClient
    // periodically buffers data from native into unity side. For that reason,
    // we're testing here if the data is correctly assembled back into one
    // piece by checking the MD5 of the downloaded data with the expected MD5.
    //
    // See DemoBigFileDownload() below for how to download directly into a file.

    string url = "http://imgsrc.hubblesite.org/hu/db/images/hs-2012-01-a-print.jpg";

    using (_client = new HTTPClient(url))
    {
      DebugClient(_client);
      while (!_client.IsDone)
      {
        DebugClient(_client);
        DebugTimeAndSpeed(_client);
        yield return null;
      }
      DebugAll(_client);

      string result;
      Error err = _client.Error;

      if ((err != null) || (_client.Response == null))
      {
        result = "ERROR: Integrity check aborted";
      }
      else
      {
        byte[] bytes = _client.Response.ContentToBytes();
        if (bytes == null)
        {
          result = "ERROR: Integrity check aborted (NO DATA RECEIVED)";
        }
        else if (bytes.Length != 3107578)
        {
          result = "ERROR: Integrity check aborted (LENGTH MISMATCH)";
        }
        else
        {
          string received = MD5Hash(bytes);
          string expected = "e6c6c2a83c5911df3aaec06408729201";

          if (received != expected)
          {
            result = "ERROR: Integrity check failed (CHECKSUM EXPECTED: " +
              expected + " BUT GOT " + received + ")";
          }
          else
          {
            result = "OK: Integrity check passed!";
          }
        }
      }

      _log = result + "\n\n" + _log;
    }
  }

  string MD5Hash(byte[] bytes)
  {
    MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
    string hash = BitConverter.ToString(md5.ComputeHash(bytes));
    return hash.Replace("-", "").ToLower();
  }

  IEnumerator DemoBigFileDownload()
  {
    _test = "DemoBigFileDownload";

    string url = "http://netstorage.unity3d.com/unity/unity-4.1.5.dmg";

    string destinationPath = Application.temporaryCachePath;
    destinationPath = System.IO.Path.Combine(destinationPath, "unity-4.1.5.dmg");

    using (_client = new HTTPClient(url, destinationPath))
    {
      DebugClient(_client);
      while (!_client.IsDone)
      {
        DebugClient(_client);
        DebugTimeAndSpeed(_client);
        yield return null;
      }
      DebugAll(_client);
    }
  }

  #region utility methods
  void DebugAll(HTTPClient _client)
  {
    DebugClient(_client);
    DebugResponseString(_client);
    DebugTimeAndSpeed(_client);
  }

  void DebugClient(HTTPClient _client)
  {
    IHTTPResponse response = _client.Response;

    _log = _client.ToString();
    if (response == null)
    {
      _log += "\nAwaiting response...";
    }
  }

  void DebugResponseString(HTTPClient _client)
  {
    if (_client.Response != null)
    {
      _log += "\n\nResponse Content\n";
      string str = _client.Response.ContentToString();
      if ((str != null) && (str.Length > 1024))
      {
        str = str.Substring(0, 1024) + "... (shortened)";
      }
      _log += str;
    }
  }

  void DebugTimeAndSpeed(HTTPClient _client)
  {
    float duration = (Time.realtimeSinceStartup - _initialTime);
    _log += "\nElapsed (secs): " + duration;

    if ((duration > 0f) && (_client.Response != null))
    {
      float speed = (_client.Response.ReceivedContentLength / 1024f) / duration;
      _log += "\nAvg Speed (KBytes/sec): " + speed;
    }
  }

  void Next()
  {
    _demoIndex = (_demoIndex + 1) % _demos.Length;
  }

  IEnumerator StartDemo()
  {
    _initialTime = Time.realtimeSinceStartup;
    _log = "";
    _curIndex = _demoIndex;
    yield return StartCoroutine(_demos[_demoIndex]());
    if (_client != null)
    {
      _client.Dispose();
      _client = null;
    }
    Next();
  }

  void OnGUI()
  {
    GUIStyle style;
    style = new GUIStyle(GUI.skin.box);
    style.wordWrap = true;
    style.padding = new RectOffset(5, 5, 5, 5);
    style.normal.textColor = Color.white;
    GUI.skin.box = style;

    Rect rect;
    rect = new Rect(0, 0, Screen.width, Screen.height);

    string text = String.Format("{0}/{1}: {2}\n\n{3}",
        _curIndex + 1, _demos.Length, _test, _log);

    GUI.Box(rect, text);

    rect = new Rect(10, Screen.height - 100, Screen.width - 20, 90);

    if (_client != null)
    {
      if (GUI.Button(rect, "CANCEL"))
      {
        _client.Cancel();
      }
    }
    else if (GUI.Button(rect, "NEXT"))
    {
      StartCoroutine(StartDemo());
    }
  }
  #endregion
}
