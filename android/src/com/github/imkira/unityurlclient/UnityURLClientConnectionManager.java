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

import java.util.HashMap;
import java.util.Map;

public class UnityURLClientConnectionManager {
    private static UnityURLClientConnectionManager instance = null;
    private static final int CONNECTION_QUEUE_CAPACITY = 32;

    private int _curConnectionId;
    private Map<Integer, UnityURLClientConnection> connectionQueue;

    public UnityURLClientConnectionManager() {
        connectionQueue = new HashMap<Integer, UnityURLClientConnection>(
            CONNECTION_QUEUE_CAPACITY);
    }

    public static synchronized UnityURLClientConnectionManager getInstance() {
        if (instance == null) {
            instance = new UnityURLClientConnectionManager();
        }
        return instance;
    }

    public synchronized UnityURLClientConnection connectionHavingID(int connectionID) {
        return connectionQueue.get(connectionID);
    }

    public synchronized void queueConnection(UnityURLClientConnection connection) {
        int id = 0;

        for (;;) {
            id = ++_curConnectionId;

            if (id == 0) {
                id = ++_curConnectionId;
            }

            if (!connectionQueue.containsKey(id)) {
                break;
            }
        }

        connection.connectionID = id;
        connectionQueue.put(id,connection);
    }

    public synchronized UnityURLClientConnection dequeueConnection(int connectionID) {
        UnityURLClientConnection connection = connectionQueue.get(connectionID);

        if (connection == null) {
            return null;
        }

        connectionQueue.remove(connectionID);
        return connection;
    }
}
