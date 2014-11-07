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

import java.io.*;
import java.util.*;
import java.net.*;
import javax.net.ssl.*;
import android.util.*;
import android.os.*;

public class UnityURLClientConnection implements Runnable {
    private static final String TAG = "UnityURLClientConnection";
    public class HttpAsyncTask extends AsyncTask<UnityURLClientConnection, Void, Boolean> {
        @Override
        protected Boolean doInBackground(UnityURLClientConnection... conn) {
            return conn[0].doInBackground();
        }

        @Override
        protected void onPostExecute(Boolean result) {
            // do nothing
        }
    }

    public class DirectByteArrayOutputStream extends ByteArrayOutputStream {
        public byte[] directBuf() {
            return this.buf;
        }
        public int directCount() {
            return this.count;
        }
    }

    public enum State {
        UnknownState,
        InitializedState,
        OpeningSourceFileState,
        OpeningDestinationFileState,
        SendingRequestState,
        SentRequestState,
        AuthenticatingState,
        ReceivingDataState,
        FinishedState,
        CancelledState,
    };

    public class Range {
        public int from;
        public int to;

        public Range(int f, int t) {
            from = f;
            to = t;
        }
    }

    public class Field {
        public String name;
        public String value;
        public List<String> values;

        public Field(String n, List<String> v) {
            name = n;

            if (name == null) {
                name = "";
            }

            values = v;

            if (v != null) {
                if (v.size() == 0) {
                    value = "";
                }

                if (v.size() == 1) {
                    value = v.get(0);
                }
                else {
                    StringBuilder str = new StringBuilder();

                    for (int i = 0; i < v.size(); ++i) {
                        if (i != 0) {
                            str.append(", ");
                        }

                        str.append(v.get(i));
                    }

                    value = str.toString();
                }
            }
            else {
                values = new ArrayList<String>();
                value = "";
            }
        }
    }

    public int connectionID;

    private HttpURLConnection _connection;
    private String _srcPath;
    private byte[] _requestContent;
    private long _requestContentLength;
    private String _dstPath;
    private boolean _dstFileResume;
    private boolean _sync_isResponseDirty;

    private ArrayList<Range> _sync_acceptableStatusCodeRanges;
    private DirectByteArrayOutputStream _sync_memoryOutputStream;
    private DirectByteArrayOutputStream _sync_pendingMemoryOutputStream;
    private FileOutputStream _sync_fileOutputStream;
    private long _sync_dstFileSize;

    private UnityURLClientError.Error _sync_error = UnityURLClientError.Error.NoneError;
    private boolean _sync_isCancelledImmediately;
    private int _sync_responseCode;
    private Map<String,List<String>> _sync_responseHeader;
    private List<Field> _responseHeader;
    private State _sync_state = State.InitializedState;
    private long _sync_responseContentLengthResumed;
    private long _sync_responseContentLengthRead;
    private long _sync_pendingResponseContentOffset;
    private long _sync_expectedContentLength;

    private static final int BUFFER_SIZE = 65536;

    private synchronized void reportError(UnityURLClientError.Error error) {
        _sync_error = error;
    }

    private synchronized boolean isCancelledImmediately() {
        return _sync_isCancelledImmediately;
    }

    private synchronized void closeOutputStreamImmediately() {
        _sync_closeOutputStreamImmediately();
    }

    private void _sync_closeOutputStreamImmediately() {
        try {
            _sync_dstFileSize = 0;

            if (_sync_fileOutputStream != null) {
                _sync_fileOutputStream.flush();
                _sync_fileOutputStream.close();
                _sync_fileOutputStream = null;
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "_sync_closeOutputStreamImmediately:" + e.toString());
        }
    }

    private void removeDestinationFile() {
        try {
            if (_dstPath != null) {
                File file = new File(_dstPath);

                if (file.exists()) {
                    file.delete();
                }
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "removeOutputPath:" + e.toString());
        }
    }

    private synchronized boolean isAcceptableStatusCode(int statusCode) {
        if (_sync_acceptableStatusCodeRanges == null) {
            return true;
        }

        for (int i = 0; i < _sync_acceptableStatusCodeRanges.size(); ++i) {
            Range range = _sync_acceptableStatusCodeRanges.get(i);

            if (range.from <= statusCode && statusCode <= range.to) {
                return true;
            }
        }

        return false;
    }

    private boolean processResponse(HttpURLConnection connection) {
        UnityURLClientDebug.d(TAG, "processResponse(0)");

        if (connection == null) {
            cancelWithError(UnityURLClientError.Error.InitConnectionError);
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(1)");

        synchronized (this) {
            if (_sync_memoryOutputStream == null && _sync_fileOutputStream == null) {
                cancelWithError(UnityURLClientError.Error.InitConnectionError);
                return false;
            }
        }

        UnityURLClientDebug.d(TAG, "processResponse(2)");

        if (isCancelledImmediately()) {
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(3)");

        try {
            if (connection != null) {
                connection.connect();
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "processResponse(): connection() failed. " + e.toString());
            cancelWithError(UnityURLClientError.Error.InitConnectionError);
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(4)");

        if (isCancelledImmediately()) {
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(5)");

        int responseCode = 0;
        Map<String,List<String>> responseHeader = null;
        long expectedContentLength = -1;

        try {
            responseCode = connection.getResponseCode();
        }
        catch (IOException e) {
            UnityURLClientDebug.e(TAG, "processResponse(): IOException: " + e.toString());
            responseCode = 401;
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "processResponse(): " + e.toString());
            cancelWithError(UnityURLClientError.Error.InitConnectionError);
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(): responseCode:" + responseCode);

        try {
            responseHeader = connection.getHeaderFields();
            String contentEncoding = connection.getContentEncoding();

            if (contentEncoding == null || !contentEncoding.equals("gzip")) {
                expectedContentLength = connection.getContentLength();
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "processResponse(): " + e.toString());
            cancelWithError(UnityURLClientError.Error.InitConnectionError);
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(6)");

        if (responseHeader != null) {
            UnityURLClientDebug.d(TAG, "responseCode:" + responseCode);

            for (Map.Entry<String,List<String>> entry : responseHeader.entrySet()) {
                UnityURLClientDebug.d(TAG, "responseHeader:" + entry.getKey() + "," + entry.getValue());
            }
        }

        synchronized (this) {
            _sync_responseCode = responseCode;
            _sync_responseHeader = responseHeader;
            _sync_expectedContentLength = expectedContentLength;

            _sync_responseContentLengthResumed = 0;
            _sync_responseContentLengthRead = 0;
            _sync_pendingResponseContentOffset = 0;

            UnityURLClientDebug.d(TAG, "processResponse: _sync_isResponseDirty is true.");
            _sync_isResponseDirty = true;

            if (_dstPath != null) {
                if (_dstFileResume) {
                    if (responseCode == 416) {
                        // Invalid resume position specified.
                        UnityURLClientDebug.e(TAG, "processResponse: Invalid resume position specified. " + _dstPath);
                        _sync_closeOutputStreamImmediately();
                        removeDestinationFile();
                        cancelWithError(UnityURLClientError.Error.InvalidResumeOffsetError);
                        return false;
                    }

                    if (responseCode == 206) {
                        // Resumable.
                    }
                    else {
                        // Non Resumable.
                        if (_sync_dstFileSize > 0) {
                            _sync_closeOutputStreamImmediately();
                            removeDestinationFile();

                            if (!openDestinationFile()) {
                                cancelWithError(UnityURLClientError.Error.CreateDestinationFileError);
                                return false;
                            }
                        }
                    }

                    _sync_responseContentLengthResumed = _sync_dstFileSize;
                }
            }
            else {
                if (_sync_memoryOutputStream != null) {
                    _sync_memoryOutputStream.reset();
                }

                if (_sync_pendingMemoryOutputStream != null) {
                    _sync_pendingMemoryOutputStream.reset();
                }
            }

            changeState(State.ReceivingDataState, true);
        }

        UnityURLClientDebug.d(TAG, "processResponse(7)");

        if (!isAcceptableStatusCode(responseCode)) {
            UnityURLClientDebug.e(TAG, "processResponse: Can't accept code." + responseCode);
            cancelWithError(UnityURLClientError.Error.UnacceptableStatusCodeError);
            return false;
        }

        UnityURLClientDebug.d(TAG, "processResponse(8)");

        UnityURLClientDebug.d(TAG, "processResponse(): responseCode:" + responseCode);
        {
            InputStream inputStream = null;
            BufferedInputStream bufferedInputStream = null;

            try {
                UnityURLClientDebug.d(TAG, "processResponse(): getInputStream.");

                try {
                    inputStream = connection.getInputStream();
                }
                catch (Exception e) {
                    // do nothing
                }

                if (inputStream == null) {
                    try {
                        inputStream = connection.getErrorStream();
                    }
                    catch (Exception e) {
                        // do nothing
                    }
                }

                if (inputStream != null) {
                    UnityURLClientDebug.d(TAG, "processResponse(): getInputStream succeeded.");
                    bufferedInputStream = new BufferedInputStream(inputStream, BUFFER_SIZE);
                    int size = 0;
                    byte buffer[] = new byte[BUFFER_SIZE];

                    while ((size = bufferedInputStream.read(buffer)) != -1) {
                        synchronized (this) {
                            if (_sync_isCancelledImmediately) {
                                return false;
                            }

                            if (_sync_memoryOutputStream != null) {
                                _sync_memoryOutputStream.write(buffer, 0, size);
                            }
                            else if (_sync_fileOutputStream != null) {
                                _sync_fileOutputStream.write(buffer, 0, size);
                            }

                            _sync_responseContentLengthRead += (long)size;
                        }
                    }
                }

                _sync_closeOutputStreamImmediately();
            }
            catch (IOException e) {
                UnityURLClientDebug.e(TAG, "processResponse(IOException):" + e.toString());
                cancelWithError(UnityURLClientError.Error.ConnectionTimeoutError);
                return false;
            }
            catch (Exception e) {
                UnityURLClientDebug.e(TAG, "processResponse(Exception):" + e.toString());
                cancelWithError(UnityURLClientError.Error.ConnectionTimeoutError);
                return false;
            }
            finally {
                try {
                    closeOutputStreamImmediately();

                    if (bufferedInputStream != null) {
                        bufferedInputStream.close();
                    }

                    if (inputStream != null) {
                        inputStream.close();
                    }
                }
                catch (Exception e) {
                    UnityURLClientDebug.e(TAG, "processResponse:" + e.toString());
                }
            }
        }

        UnityURLClientDebug.d(TAG, "processResponse(9)");
        changeState(State.FinishedState, false);
        return true;
    }

    public Boolean doInBackground() {
        UnityURLClientDebug.d(TAG, "doInBackground(0)");

        synchronized (this) {
            if (_sync_isCancelledImmediately) {
                return false;
            }
        }

        UnityURLClientDebug.d(TAG, "doInBackground(1)");
        UnityURLClientDebug.d(TAG, "doInBackground(2)");
        HttpURLConnection connection = _connection;
        UnityURLClientDebug.d(TAG, "doInBackground(3)");
        boolean r = processResponse(connection);

        try {
            if (connection != null) {
                connection.disconnect();
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "doInBackground:" + e.toString());
        }

        UnityURLClientDebug.d(TAG, "doInBackground(4)");
        closeOutputStreamImmediately();
        UnityURLClientDebug.d(TAG, "doInBackground(5)");
        return r;
    }

    public UnityURLClientConnection(String method, String url, int cachePolicy, float timeout) {
        UnityURLClientDebug.d(TAG, "UnityURLClientConnection:" + method + " URL:" + url + " cachePolicy:" + cachePolicy + " timeout:" + timeout);

        _sync_state = State.InitializedState;

        try {
            URL connectionURL = new URL(url);
            _connection = (HttpURLConnection)connectionURL.openConnection();
            _connection.setRequestMethod(method);

            if (timeout > 0.0f) {
                _connection.setConnectTimeout((int)(timeout * 1000.0f));
                _connection.setReadTimeout((int)(timeout * 1000.0f));
            }

            if (cachePolicy != 0) {
                _connection.setUseCaches(false);
            }

            if (method != null && method.equals("POST")) {
                _connection.setDoOutput(true);
                _connection.setChunkedStreamingMode(0);
            }

            _connection.setRequestProperty("Connection", "close");
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "UnityURLClientConnection:" + e.toString());
            _sync_error = UnityURLClientError.Error.AllocationError;
            _connection = null;
        }
    }

    public synchronized int getState() {
        return _sync_state.ordinal();
    }

    public synchronized String getErrorDomain() {
        if (_sync_error == UnityURLClientError.Error.NoneError) {
            return null;
        }

        return "";
    }

    public synchronized long getErrorCode() {
        UnityURLClientDebug.d(TAG, "getErrorCode:" + _sync_error);
        return _sync_error.ordinal();
    }

    public synchronized String getErrorDescription() {
        UnityURLClientDebug.d(TAG, "getErrorDescription:" + _sync_error);

        if (_sync_error == UnityURLClientError.Error.NoneError) {
            return null;
        }

        return UnityURLClientError.getErrorDescriptions(_sync_error.ordinal());
    }

    public void setAllowFollowRedirects(boolean arrow, int maxCount) {
        UnityURLClientDebug.d(TAG, "setAllowFollowRedirects");

        if (changeState(State.InitializedState, true)) {
            if (_connection != null) {
                _connection.setInstanceFollowRedirects(arrow);
            }
        }
        else {
            UnityURLClientDebug.e(TAG, "setAllowFollowRedirects: changeState:" + _sync_state);
        }
    }

    public void setAllowInvalidSSLCertificate(boolean arrow) {
        UnityURLClientDebug.d(TAG, "setAllowInvalidSSLCertificate");

        if (changeState(State.InitializedState, true)) {
            if (_connection != null) {
                if (arrow) {
                    try {
                        javax.net.ssl.KeyManager[] km = null;
                        javax.net.ssl.TrustManager[] tm = {
                            new javax.net.ssl.X509TrustManager() {
                                public void checkClientTrusted(java.security.cert.X509Certificate[] arg0, String arg1) throws java.security.cert.CertificateException {}
                                public void checkServerTrusted(java.security.cert.X509Certificate[] arg0, String arg1) throws java.security.cert.CertificateException {}
                                public java.security.cert.X509Certificate[] getAcceptedIssuers() {
                                    return null;
                                }
                            }
                        };
                        javax.net.ssl.SSLContext sslcontext= javax.net.ssl.SSLContext.getInstance("SSL");
                        sslcontext.init(km, tm, new java.security.SecureRandom());
                        ((HttpsURLConnection)_connection).setSSLSocketFactory(sslcontext.getSocketFactory());
                        ((HttpsURLConnection)_connection).setHostnameVerifier(
                        new javax.net.ssl.HostnameVerifier() {
                            public boolean verify(String host, javax.net.ssl.SSLSession ses) {
                                return true;
                            }
                        }
                        );
                    }
                    catch (Exception e) {
                    }
                }
            }
        }
        else {
            UnityURLClientDebug.e(TAG, "setAllowInvalidSSLCertificate: changeState:" + _sync_state);
        }
    }

    public void setRequestContentSource(String srcPath) {
        UnityURLClientDebug.d(TAG, "setRequestContentSource");

        if (changeState(State.InitializedState, true)) {
            _srcPath = srcPath;
            _requestContent = null;
            _requestContentLength = 0;
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestContentSource: changeState:" + _sync_state);
        }
    }

    public void setRequestHeader(String name, String value) {
        UnityURLClientDebug.d(TAG, "setRequestHeader");

        if (changeState(State.InitializedState, true)) {
            if (_connection != null) {
                _connection.setRequestProperty(name, value);
            }
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestHeader: changeState:" + _sync_state);
        }
    }

    public void setRequestAuthCredential(String user, String password) {
        UnityURLClientDebug.d(TAG, "setRequestAuthCredential");

        if (changeState(State.InitializedState, true)) {
            if (_connection != null) {
                String userpass = user + ":" + password;
                String basicAuth = "Basic " + new String(Base64.encode(userpass.getBytes(), Base64.DEFAULT));
                _connection.setRequestProperty("Authorization", basicAuth);
            }
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestAuthCredential: changeState:" + _sync_state);
        }
    }

    public void addAcceptableResponseStatusCodeRange(long from, long to) {
        UnityURLClientDebug.d(TAG, "addAcceptableResponseStatusCodeRange");

        if (changeState(State.InitializedState, true)) {
            synchronized (this) {
                if (_sync_acceptableStatusCodeRanges == null) {
                    _sync_acceptableStatusCodeRanges = new ArrayList<Range>();
                }

                _sync_acceptableStatusCodeRanges.add(new Range((int)from, (int)to));
            }
        }
        else {
            UnityURLClientDebug.e(TAG, "addAcceptableResponseStatusCodeRange: changeState:" + _sync_state);
        }
    }

    public void setResponseContentDestination(String dstPath, boolean allowResume) {
        UnityURLClientDebug.d(TAG, "setResponseContentDestination");

        if (changeState(State.InitializedState, true)) {
            _dstPath = dstPath;
            _dstFileResume = allowResume;
        }
        else {
            UnityURLClientDebug.e(TAG, "setResponseContentDestination: changeState:" + _sync_state);
        }
    }

    private boolean setRequestHTTPBody() {
        if (_connection == null) {
            return false;
        }

        try {
            if (_srcPath != null) {
                if (!changeState(State.OpeningSourceFileState, false)) {
                    return false;
                }

                OutputStream outputStream = _connection.getOutputStream();
                FileInputStream inputFileStream = new FileInputStream(_srcPath);
                int size = 0;
                byte[] buffer = new byte[BUFFER_SIZE];

                while ((size = inputFileStream.read(buffer)) != -1) {
                    outputStream.write(buffer, 0, size);
                }

                inputFileStream.close();
                outputStream.flush();
                outputStream.close();
                _srcPath = null;
            }
            else if (_requestContent != null && _requestContentLength > 0) {
                OutputStream outputStream = _connection.getOutputStream();
                outputStream.write(_requestContent, 0, (int)_requestContentLength);
                outputStream.flush();
                outputStream.close();
                _requestContent = null;
                _requestContentLength = 0;
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "setRequestHTTPBody:" + e.toString());
            reportError(UnityURLClientError.Error.OpenSourceFileError);
            return false;
        }

        return true;
    }

    private boolean openDestinationFile() {
        if (_sync_fileOutputStream != null) {
            return true;
        }

        try {
            File file = new File(_dstPath);
            long dstFileSize = file.length();

            if (dstFileSize > 0 && !_dstFileResume) {
                dstFileSize = 0;
                file.delete();
            }

            FileOutputStream fileOutputStream = new FileOutputStream(_dstPath, true);

            synchronized (this) {
                _sync_fileOutputStream = fileOutputStream;
                _sync_dstFileSize = dstFileSize;
            }
        }
        catch (Exception e) {
            UnityURLClientDebug.e(TAG, "openDestinationFile:" + e.toString());
            reportError(UnityURLClientError.Error.OpenDestinationFileError);
            return false;
        }

        return true;
    }

    private void setResponseContentDestinationResumeHeader() {
        synchronized (this) {
            if (_sync_dstFileSize > 0) {
                if (_connection != null) {
                    _connection.setRequestProperty("Range", "bytes=" + _sync_dstFileSize + "-");
                }
            }
        }
    }

    public void sendRequest() {
        UnityURLClientDebug.d(TAG, "sendRequest");

        if (_connection == null) {
            UnityURLClientDebug.e(TAG, "sendRequest: Connection is null.");
            return;
        }

        if (!setRequestHTTPBody()) {
            UnityURLClientDebug.e(TAG, "sendRequest: Failed setRequestHTTPBody().");
            return;
        }

        if (_dstPath != null) {
            if (!changeState(State.OpeningDestinationFileState, false)) {
                UnityURLClientDebug.e(TAG, "sendRequest: Failed change OpeningDestinationFileState.");
                return;
            }

            if (!openDestinationFile()) {
                UnityURLClientDebug.e(TAG, "sendRequest: Failed openDestinationFile().");
                return;
            }
        }

        if (!changeState(State.SendingRequestState, false)) {
            UnityURLClientDebug.e(TAG, "sendRequest: Failed change SendingRequestState.");
            return;
        }

        if (_dstPath != null) {
            if (_dstFileResume) {
                setResponseContentDestinationResumeHeader();
            }
        }

        if (!changeState(State.SentRequestState, false)) {
            UnityURLClientDebug.e(TAG, "sendRequest: Failed change SentRequestState.");
            return;
        }

        if (_dstPath == null) {
            synchronized (this) {
                _sync_memoryOutputStream = new DirectByteArrayOutputStream();
                _sync_pendingMemoryOutputStream = new DirectByteArrayOutputStream();
            }
        }

        UnityURLClientDebug.d(TAG, "sendRequest: post.");
        Handler handler = new Handler(Looper.getMainLooper());
        handler.post(this);
    }

    public void run() {
        UnityURLClientDebug.d(TAG, "run");
        new HttpAsyncTask().execute(this);
    }

    public long getResponseStatusCode() {
        UnityURLClientDebug.d(TAG, "getResponseStatusCode");

        synchronized (this) {
            return (long)_sync_responseCode;
        }
    }

    private void _PrepareResponseHeader() {
        if (_responseHeader == null) {
            synchronized (this) {
                if (_sync_responseHeader != null) {
                    _responseHeader = new ArrayList<Field>();

                    for (Map.Entry<String,List<String>> entry : _sync_responseHeader.entrySet()) {
                        _responseHeader.add(new Field(entry.getKey(), entry.getValue()));
                    }
                }
            }
        }
    }

    public String getResponseHeaderName(int index) {
        UnityURLClientDebug.d(TAG, "getResponseHeaderName");
        _PrepareResponseHeader();

        if (_responseHeader != null && index >= 0 && index < _responseHeader.size()) {
            return _responseHeader.get(index).name;
        }

        return null;
    }

    public String getResponseHeaderValue(int index) {
        UnityURLClientDebug.d(TAG, "getResponseHeaderValue");
        _PrepareResponseHeader();

        if (_responseHeader != null && index >= 0 && index < _responseHeader.size()) {
            return _responseHeader.get(index).value;
        }

        return null;
    }

    public int getResponseRedirectCount() {
        UnityURLClientDebug.d(TAG, "getResponseRedirectCount");
        return -1;
    }

    public synchronized long getResponseContentLengthRead() {
        UnityURLClientDebug.d(TAG, "getResponseContentLengthRead:" + _sync_responseContentLengthRead);
        return _sync_responseContentLengthRead;
    }

    public synchronized long getResponseContentExpectedLength() {
        UnityURLClientDebug.d(TAG, "getResponseContentExpectedLength:" + _sync_expectedContentLength);
        return _sync_expectedContentLength;
    }

    public synchronized long getResponseContentLengthResumed() {
        UnityURLClientDebug.d(TAG, "getResponseContentLengthResumed:" + _sync_responseContentLengthResumed);
        return _sync_responseContentLengthResumed;
    }

    public synchronized long getPendingResponseContentLength() {
        UnityURLClientDebug.d(TAG, "getPendingResponseContentLength");
        long length = 0;

        if (_sync_pendingMemoryOutputStream != null) {
            length += (long)_sync_pendingMemoryOutputStream.size() - _sync_pendingResponseContentOffset;
            UnityURLClientDebug.d(TAG, "getPendingResponseContentLength:" + _sync_pendingMemoryOutputStream.size() + " offset:" + _sync_pendingResponseContentOffset);
        }

        if (_sync_memoryOutputStream != null) {
            length += (long)_sync_memoryOutputStream.size();
            UnityURLClientDebug.d(TAG, "getPendingResponseContentLength:" + _sync_memoryOutputStream.size());
        }

        UnityURLClientDebug.d(TAG, "getPendingResponseContentLength:" + length);
        return length;
    }

    public synchronized boolean checkAndResetResponseDirtyFlag() {
        UnityURLClientDebug.d(TAG, "checkAndResetResponseDirtyFlag");

        if (_sync_isResponseDirty) {
            _sync_isResponseDirty = false;
            UnityURLClientDebug.d(TAG, "checkAndResetResponseDirtyFlag: true.");
            return true;
        }

        UnityURLClientDebug.d(TAG, "checkAndResetResponseDirtyFlag: false.");
        return false;
    }

    public void cancel() {
        UnityURLClientDebug.d(TAG, "cancel");

        synchronized (this) {
            changeState(State.CancelledState, false);
            _sync_isCancelledImmediately = true;
            _sync_closeOutputStreamImmediately();
        }
    }

    public void cancelWithError(UnityURLClientError.Error error) {
        UnityURLClientDebug.d(TAG, "cancelWithError:" + error);

        synchronized (this) {
            changeState(State.CancelledState, false);
            _sync_isCancelledImmediately = true;
            _sync_error = error;
            _sync_closeOutputStreamImmediately();
        }
    }

    public void setRequestContent(byte[] src, long srcLength) {
        UnityURLClientDebug.d(TAG, "setRequestContent");

        if (changeState(State.InitializedState, true)) {
            _srcPath = null;
            _requestContent = src;
            _requestContentLength = srcLength;
        }
        else {
            UnityURLClientDebug.e(TAG, "setRequestContent: changeState:" + _sync_state);
        }
    }

    private static long copyResponseContent(DirectByteArrayOutputStream srcData, long srcOffset, byte[] dst, long dstCapacity) {
        if (dst == null || dstCapacity <= 0 || srcData == null) {
            return 0;
        }

        byte[] directBuf = srcData.directBuf();

        if (directBuf == null) {
            return 0;
        }

        long length = (long)srcData.size();

        if (length <= 0 || srcOffset >= length) {
            return 0;
        }

        length = length - srcOffset;
        length = (length < dstCapacity) ? length : dstCapacity;
        System.arraycopy(directBuf, (int)srcOffset, dst, 0, (int)length);
        return length;
    }

    public synchronized long movePendingResponseContent(byte[] dst, long dstCapacity) {
        UnityURLClientDebug.d(TAG, "movePendingResponseContent: dstCapacity:" + dstCapacity);

        if (_sync_isResponseDirty) {
            UnityURLClientDebug.d(TAG, "movePendingResponseContent: isResponseDirty is true.");
            return 0;
        }

        {
            DirectByteArrayOutputStream pendingStream = _sync_pendingMemoryOutputStream;

            if (pendingStream != null && pendingStream.size() > 0) {
                UnityURLClientDebug.d(TAG, "movePendingResponseContent: pendingStream.size():" + pendingStream.size());
                long copied = copyResponseContent(pendingStream, _sync_pendingResponseContentOffset, dst, dstCapacity);

                // no data copied?
                if (copied <= 0) {
                    UnityURLClientDebug.d(TAG, "movePendingResponseContent: copied is zero.");
                    return copied;
                }

                // all data copied?
                _sync_pendingResponseContentOffset += copied;

                if (_sync_pendingResponseContentOffset >= pendingStream.size()) {
                    UnityURLClientDebug.d(TAG, "movePendingResponseContent: resetStream:"
                                          + " pendingResponseContentOffset" + _sync_pendingResponseContentOffset
                                          + " pendingStream.size():" + pendingStream.size()
                                          + " copied:" + copied);
                    pendingStream.reset();
                    _sync_pendingResponseContentOffset = 0;
                }
                else {
                    UnityURLClientDebug.d(TAG, "movePendingResponseContent: not resetStream:"
                                          + " pendingResponseContentOffset:" + _sync_pendingResponseContentOffset
                                          + " pendingStream.size():" + pendingStream.size()
                                          + " copied:" + copied);
                }

                UnityURLClientDebug.d(TAG, "movePendingResponseContent: copied:" + copied);
                return copied;
            }
        }

        {
            DirectByteArrayOutputStream stream = _sync_memoryOutputStream;

            if (stream != null && stream.size() > 0) {
                UnityURLClientDebug.d(TAG, "movePendingResponseContent: stream.size():" + stream.size());
                long copied = copyResponseContent(stream, 0, dst, dstCapacity);

                // no data copied?
                if (copied <= 0) {
                    UnityURLClientDebug.d(TAG, "movePendingResponseContent(2): copied is zero.");
                    return copied;
                }

                // all data copied?
                if (copied >= stream.size()) {
                    UnityURLClientDebug.d(TAG, "movePendingResponseContent(2): stream.reset():"
                                          + " stream.size():" + stream.size()
                                          + " copied:" + copied);
                    stream.reset();
                    return copied;
                }

                DirectByteArrayOutputStream pendingStream = _sync_pendingMemoryOutputStream;

                if (pendingStream == null) {
                    pendingStream = new DirectByteArrayOutputStream();
                }
                else {
                    pendingStream.reset();
                }

                _sync_memoryOutputStream = pendingStream;
                _sync_pendingMemoryOutputStream = stream;
                _sync_pendingResponseContentOffset = copied;

                UnityURLClientDebug.d(TAG, "movePendingResponseContent(2): copied:" + copied
                                      + " pendingStream.size():" + pendingStream.size()
                                      + " stream.size():" + stream.size());

                return copied;
            }
        }

        UnityURLClientDebug.d(TAG, "movePendingResponseContent(3): Nothing.");
        return 0;
    }

    private synchronized boolean changeState(State newState, boolean allowSame) {
        if (_sync_state.ordinal() <= State.UnknownState.ordinal()) {
            UnityURLClientDebug.e(TAG, "changeState: UnknownState:" + _sync_state);
            return false;
        }

        if (_sync_state == newState) {
            UnityURLClientDebug.d(TAG, "changeState: allowSame:" + allowSame);
            return allowSame;
        }

        if ((_sync_state.ordinal() >= newState.ordinal()) || (_sync_state.ordinal() >= State.FinishedState.ordinal())) {
            UnityURLClientDebug.d(TAG, "changeState: UnknownFlow:" + _sync_state + " New:" + newState);
            return false;
        }

        if (_sync_error != UnityURLClientError.Error.NoneError) {
            UnityURLClientDebug.e(TAG, "changeState: HasError:" + _sync_error);
            return false;
        }

        _sync_state = newState;
        return true;
    }
}
