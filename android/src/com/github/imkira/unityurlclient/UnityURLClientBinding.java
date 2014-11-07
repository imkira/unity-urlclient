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


public class UnityURLClientBinding {
    private static final String TAG = "UnityURLClientBinding";
    private static UnityURLClientBinding instance = null;

    private UnityURLClientConnectionManager _manager = null;

    public UnityURLClientBinding() {
        _manager = new UnityURLClientConnectionManager();
    }

    public static synchronized UnityURLClientBinding getInstance() {
        UnityURLClientDebug.d(TAG, "getInstance");

        if (instance == null) {
            instance = new UnityURLClientBinding();
        }

        return instance;
    }

    public void setDebug(boolean isDebug) {
        UnityURLClientDebug.DEBUG = isDebug;
    }

    public int createHTTPConnection(String method, String url, int cachePolicy, float timeout) {
        UnityURLClientConnection connection = new UnityURLClientConnection(method, url, cachePolicy, timeout);
        _manager.queueConnection(connection);
        return connection.connectionID;
    }

    public int getState(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getState();
        }

        UnityURLClientDebug.e(TAG, "getState: Connection not found: " + connectionID);
        return 0;
    }

    public String getErrorDomain(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getErrorDomain();
        }

        UnityURLClientDebug.e(TAG, "getErrorDomain: Connection not found: " + connectionID);
        return "";
    }

    public long getErrorCode(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getErrorCode();
        }

        UnityURLClientDebug.e(TAG, "getErrorCode: Connection not found: " + connectionID);
        return 0;
    }

    public String getErrorDescription(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getErrorDescription();
        }

        UnityURLClientDebug.e(TAG, "getErrorDescription: Connection not found: " + connectionID);
        return "Unknown";
    }

    public void setAllowFollowRedirects(int connectionID, boolean arrow, int maxCount) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setAllowFollowRedirects(arrow, maxCount);
        }
        else {
            UnityURLClientDebug.e(TAG, "setAllowFollowRedirects: Connection not found: " + connectionID);
        }
    }

    public void setAllowInvalidSSLCertificate(int connectionID, boolean arrow) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setAllowInvalidSSLCertificate(arrow);
        }
        else {
            UnityURLClientDebug.e(TAG, "setAllowInvalidSSLCertificate: Connection not found: " + connectionID);
        }
    }

    public void setRequestContentSource(int connectionID, String srcPath) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setRequestContentSource(srcPath);
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestContentSource: Connection not found: " + connectionID);
        }
    }

    public void setRequestHeader(int connectionID, String name, String value) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setRequestHeader(name, value);
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestHeader: Connection not found: " + connectionID);
        }
    }

    public void setRequestAuthCredential(int connectionID, String user, String password) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setRequestAuthCredential(user, password);
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestAuthCredential: Connection not found: " + connectionID);
        }
    }

    public void addAcceptableResponseStatusCodeRange(int connectionID, long from, long to) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.addAcceptableResponseStatusCodeRange(from, to);
        }
        else {
            UnityURLClientDebug.e(TAG, "addAcceptableResponseStatusCodeRange: Connection not found: " + connectionID);
        }
    }

    public void setResponseContentDestination(int connectionID, String dstPath, boolean allowResume) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setResponseContentDestination(dstPath, allowResume);
        }
        else {
            UnityURLClientDebug.e(TAG, "setResponseContentDestination: Connection not found: " + connectionID);
        }
    }

    public void sendRequest(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.sendRequest();
        }
        else {
            UnityURLClientDebug.e(TAG, "sendRequest: Connection not found: " + connectionID);
        }
    }

    public long getResponseStatusCode(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseStatusCode();
        }

        UnityURLClientDebug.e(TAG, "getResponseStatusCode: Connection not found: " + connectionID);
        return 0;
    }

    public String getResponseHeaderName(int connectionID, int index) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseHeaderName(index);
        }

        UnityURLClientDebug.e(TAG, "getResponseHeaderName: Connection not found: " + connectionID);
        return null;
    }

    public String getResponseHeaderValue(int connectionID, int index) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseHeaderValue(index);
        }

        UnityURLClientDebug.e(TAG, "getResponseHeaderValue: Connection not found: " + connectionID);
        return null;
    }

    public int getResponseRedirectCount(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseRedirectCount();
        }

        UnityURLClientDebug.e(TAG, "getResponseRedirectCount: Connection not found: " + connectionID);
        return -1;
    }

    public long getResponseContentLengthRead(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseContentLengthRead();
        }

        UnityURLClientDebug.e(TAG, "getResponseContentLengthRead: Connection not found: " + connectionID);
        return 0;
    }

    public long getResponseContentExpectedLength(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseContentExpectedLength();
        }

        UnityURLClientDebug.e(TAG, "getResponseContentExpectedLength: Connection not found: " + connectionID);
        return -1;
    }

    public long getResponseContentLengthResumed(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getResponseContentLengthResumed();
        }

        UnityURLClientDebug.e(TAG, "getResponseContentLengthResumed: Connection not found: " + connectionID);
        return 0;
    }

    public long getPendingResponseContentLength(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.getPendingResponseContentLength();
        }

        UnityURLClientDebug.e(TAG, "getPendingResponseContentLength: Connection not found: " + connectionID);
        return 0;
    }

    public boolean checkAndResetResponseDirtyFlag(int connectionID) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.checkAndResetResponseDirtyFlag();
        }

        UnityURLClientDebug.e(TAG, "checkAndResetResponseDirtyFlag: Connection not found: " + connectionID);
        return false;
    }

    public void destroyConnection(int connectionID) {
        UnityURLClientConnection connection = _manager.dequeueConnection(connectionID);

        if (connection != null) {
            connection.cancel();
        }
        else {
            UnityURLClientDebug.e(TAG, "destroyConnection: Connection not found: " + connectionID);
        }
    }

    public void setRequestContent(int connectionID, byte[] src, long srcLength) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            connection.setRequestContent(src, srcLength);
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestContent: Connection not found: " + connectionID);
        }
    }

    public long movePendingResponseContent(int connectionID, byte[] dst, long dstLength) {
        UnityURLClientConnection connection = _manager.connectionHavingID(connectionID);

        if (connection != null) {
            return connection.movePendingResponseContent(dst, dstLength);
        }

        UnityURLClientDebug.e(TAG, "movePendingResponseContent: Connection not found: " + connectionID);
        return 0;
    }
}
