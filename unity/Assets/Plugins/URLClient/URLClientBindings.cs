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

using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace URLClient
{
  public enum CachePolicy
  {
    UseProtocolCachePolicy = 0,
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IPHONE
    ReloadIgnoringLocalCacheData = 1,
    ReturnCacheDataElseLoad = 2,
    ReturnCacheDataDontLoad = 3,
#endif
    ReloadIgnoringLocalAndRemoteCacheData = 4,
    ReloadRevalidatingCacheData = 5
  }

  public enum ConnectionState
  {
    Unknown = 0,
    Initialized = 1,
    OpeningSourceFile = 2,
    OpeningDestinationFile = 3,
    SendingRequest = 4,
    SentRequest = 5,
    AuthenticatingState = 6,
    ReceivingData = 7,
    Finished = 8,
    Cancelled = 9
  };

  public class Bindings : MonoBehaviour
  {
    public const string ERROR_DOMAIN = "UnityURLClient";

    public const uint INVALID_CONNECTION_ID = 0;

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IPHONE

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern uint _URLClientCreateHTTPConnection(string method,
        string url, CachePolicy cachePolicy, float timeout);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    private static extern int _URLClientGetState(uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern string _URLClientGetErrorDomain(uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern long _URLClientGetErrorCode(uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern string _URLClientGetErrorDescription(uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetAllowFollowRedirects(
        uint connectionID, bool allow, int maxCount);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetAllowInvalidSSLCertificate(
        uint connectionID, bool allow);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    private static extern void _URLClientSetRequestContent(
        uint connectionID, IntPtr src, ulong srcLength);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetRequestContentSource(
        uint connectionID, string srcPath);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetRequestHeader(uint connectionID,
        string name, string value);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetRequestAuthCredential(
        uint connectionID, string user, string password);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientAddAcceptableResponseStatusCodeRange(
        uint connectionID, long from, long to);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSetResponseContentDestination(
        uint connectionID, string dstPath, bool allowResume);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientSendRequest(uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern long _URLClientGetResponseStatusCode(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern string _URLClientGetResponseHeaderName(
        uint connectionID, uint index);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern string _URLClientGetResponseHeaderValue(
        uint connectionID, uint index);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern int _URLClientGetResponseRedirectCount(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern ulong _URLClientGetResponseContentLengthRead(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern long _URLClientGetResponseContentExpectedLength(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern ulong _URLClientGetResponseContentLengthResumed(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern ulong _URLClientGetPendingResponseContentLength(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern bool _URLClientCheckAndResetResponseDirtyFlag(
        uint connectionID);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    private static extern ulong _URLClientMovePendingResponseContent(
        uint connectionID, IntPtr dst, ulong dstLength);

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
    [DllImport("URLClient")]
#else
    [DllImport("__Internal")]
#endif
    public static extern void _URLClientDestroyConnection(uint connectionID);

#elif UNITY_ANDROID
    private static AndroidJavaObject _unityURLClientBindingInstance = null;
    private static AndroidJavaObject UnityURLClientBindingInstance
    {
      get
      {
        if (_unityURLClientBindingInstance == null)
        {
          AndroidJavaClass unityURLClientBindingClass;

          unityURLClientBindingClass =
            new AndroidJavaClass("com.github.imkira.unityurlclient.UnityURLClientBinding");
          _unityURLClientBindingInstance =
            unityURLClientBindingClass.CallStatic<AndroidJavaObject>("getInstance");
#if DEBUG
          if (_unityURLClientBindingInstance != null)
          {
            _unityURLClientBindingInstance.Call("setDebug", true);
          }
#endif
        }
        return _unityURLClientBindingInstance;
      }
    }
    
    public static uint _URLClientCreateHTTPConnection(string method,
        string url, CachePolicy cachePolicy, float timeout)
    {
      return (uint)UnityURLClientBindingInstance.Call<int>("createHTTPConnection",
          method, url, (int)cachePolicy, timeout);
    }

    private static int _URLClientGetState(uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<int>("getState",
          (int)connectionID);
    }

    public static string _URLClientGetErrorDomain(uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<string>("getErrorDomain",
          (int)connectionID);
    }

    public static long _URLClientGetErrorCode(uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<long>("getErrorCode",
          (int)connectionID);
    }

    public static string _URLClientGetErrorDescription(uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<string>("getErrorDescription",
          (int)connectionID);
    }

    public static void _URLClientSetAllowFollowRedirects(
        uint connectionID, bool allow, int maxCount)
    {
      UnityURLClientBindingInstance.Call("setAllowFollowRedirects",
          (int)connectionID, allow, maxCount);
    }

    public static void _URLClientSetAllowInvalidSSLCertificate(
        uint connectionID, bool allow)
    {
      UnityURLClientBindingInstance.Call("setAllowInvalidSSLCertificate",
          (int)connectionID, allow);
    }
#if false
    private static void _URLClientSetRequestContent(
        uint connectionID, IntPtr src, ulong srcLength)
    {
    }
#endif
    public static void _URLClientSetRequestContentSource(
        uint connectionID, string srcPath)
    {
      UnityURLClientBindingInstance.Call("setRequestContentSource",
          (int)connectionID, srcPath);
    }

    public static void _URLClientSetRequestHeader(uint connectionID,
        string name, string value)
    {
      UnityURLClientBindingInstance.Call("setRequestHeader",
          (int)connectionID, name, value);
    }

    public static void _URLClientSetRequestAuthCredential(
        uint connectionID, string user, string password)
    {
      UnityURLClientBindingInstance.Call("setRequestAuthCredential",
          (int)connectionID, user, password);
    }

    public static void _URLClientAddAcceptableResponseStatusCodeRange(
        uint connectionID, long from, long to)
    {
      UnityURLClientBindingInstance.Call("addAcceptableResponseStatusCodeRange",
          (int)connectionID, from, to);
    }

    public static void _URLClientSetResponseContentDestination(
        uint connectionID, string dstPath, bool allowResume)
    {
      UnityURLClientBindingInstance.Call("setResponseContentDestination",
          (int)connectionID, dstPath, allowResume);
    }

    public static void _URLClientSendRequest(uint connectionID)
    {
      UnityURLClientBindingInstance.Call("sendRequest",
          (int)connectionID);
    }

    public static long _URLClientGetResponseStatusCode(
        uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<long>("getResponseStatusCode",
          (int)connectionID);
    }

    public static string _URLClientGetResponseHeaderName(
        uint connectionID, uint index)
    {
      return UnityURLClientBindingInstance.Call<string>("getResponseHeaderName",
          (int)connectionID, (int)index);
    }

    public static string _URLClientGetResponseHeaderValue(
        uint connectionID, uint index)
    {
      return UnityURLClientBindingInstance.Call<string>("getResponseHeaderValue",
          (int)connectionID, (int)index);
    }

    public static int _URLClientGetResponseRedirectCount(
        uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<int>("getResponseRedirectCount",
          (int)connectionID);
    }

    public static ulong _URLClientGetResponseContentLengthRead(
        uint connectionID)
    {
      return (ulong)UnityURLClientBindingInstance.Call<long>("getResponseContentLengthRead",
          (int)connectionID);
    }

    public static long _URLClientGetResponseContentExpectedLength(
        uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<long>("getResponseContentExpectedLength",
          (int)connectionID);
    }

    public static ulong _URLClientGetResponseContentLengthResumed(
        uint connectionID)
    {
      return (ulong)UnityURLClientBindingInstance.Call<long>("getResponseContentLengthResumed",
          (int)connectionID);
    }

    public static ulong _URLClientGetPendingResponseContentLength(
        uint connectionID)
    {
      return (ulong)UnityURLClientBindingInstance.Call<long>("getPendingResponseContentLength",
          (int)connectionID);
    }

    public static bool _URLClientCheckAndResetResponseDirtyFlag(
        uint connectionID)
    {
      return UnityURLClientBindingInstance.Call<bool>("checkAndResetResponseDirtyFlag",
          (int)connectionID);
    }
#if false
    private static ulong _URLClientMovePendingResponseContent(
        uint connectionID, IntPtr dst, ulong dstLength)
    {
      return 0;
    }
#endif
    public static void _URLClientDestroyConnection(uint connectionID)
    {
      UnityURLClientBindingInstance.Call("destroyConnection",
          (int)connectionID);
    }

#else
#endif

    public static ConnectionState URLClientGetState(uint connectionID)
    {
      int state = _URLClientGetState(connectionID);

      if ((state < (int)ConnectionState.Initialized) ||
          (state > (int)ConnectionState.Cancelled))
      {
        return ConnectionState.Unknown;
      }

      return (ConnectionState)state;
    }

    public static void URLClientSetRequestContent(uint connectionID,
        byte[] src, ulong srcLength)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IPHONE
      GCHandle pinnedArray = GCHandle.Alloc(src, GCHandleType.Pinned);
      IntPtr ptrSrc = pinnedArray.AddrOfPinnedObject();
      _URLClientSetRequestContent(connectionID, ptrSrc, srcLength);
      pinnedArray.Free();
#elif UNITY_ANDROID
      IntPtr rawClass = UnityURLClientBindingInstance.GetRawClass();
      IntPtr rawObject = UnityURLClientBindingInstance.GetRawObject();
      
      IntPtr methodPtr = AndroidJNI.GetMethodID(rawClass, "setRequestContent", "(I[BJ)V");
      
      IntPtr v2 = AndroidJNI.ToByteArray( src );
      jvalue j1 = new jvalue();
      j1.i = (int)connectionID;
      jvalue j2 = new jvalue();
      j2.l = v2;
      jvalue j3 = new jvalue();
      j3.j = (long)srcLength;
   
      AndroidJNI.CallVoidMethod( rawObject, methodPtr, new jvalue[]{j1, j2, j3} );
      AndroidJNI.DeleteLocalRef(v2);
#endif
    }

    public static ulong URLClientMovePendingResponseContent(uint connectionID,
        byte[] dst, ulong dstLength)
    {
      ulong length;
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_IPHONE
      GCHandle pinnedArray = GCHandle.Alloc(dst, GCHandleType.Pinned);
      IntPtr ptrDst = pinnedArray.AddrOfPinnedObject();
      length = _URLClientMovePendingResponseContent(connectionID, ptrDst,
          dstLength);
      pinnedArray.Free();
#elif UNITY_ANDROID
      IntPtr rawClass = UnityURLClientBindingInstance.GetRawClass();
      IntPtr rawObject = UnityURLClientBindingInstance.GetRawObject();
      
      IntPtr methodPtr = AndroidJNI.GetMethodID(rawClass, "movePendingResponseContent", "(I[BJ)J");
      
      IntPtr v2 = AndroidJNI.ToByteArray( dst );
      jvalue j1 = new jvalue();
      j1.i = (int)connectionID;
      jvalue j2 = new jvalue();
      j2.l = v2;
      jvalue j3 = new jvalue();
      j3.j = (long)dstLength;
      
      length = (ulong)AndroidJNI.CallLongMethod( rawObject, methodPtr, new jvalue[]{j1, j2, j3} );
      if (dst != null)
      {
        byte[] resultDst = AndroidJNI.FromByteArray( v2 );
        for( ulong n = 0; n < length; ++n ) {
          dst[n] = resultDst[n];
        }
      }
      AndroidJNI.DeleteLocalRef(v2);
#endif
      return length;
    }
  }
}
