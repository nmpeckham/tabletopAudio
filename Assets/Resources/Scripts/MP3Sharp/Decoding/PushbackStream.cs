// /***************************************************************************
//  * PushbackStream.cs
//  * Copyright (c) 2015, 2021 The Authors.
//  * 
//  * All rights reserved. This program and the accompanying materials
//  * are made available under the terms of the GNU Lesser General Public License
//  * (LGPL) version 3 which accompanies this distribution, and is available at
//  * https://www.gnu.org/licenses/lgpl-3.0.en.html
//  *
//  * This library is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  * Lesser General Public License for more details.
//  *
//  ***************************************************************************/

using System;
using System.IO;

namespace MP3Sharp.Decoding {
    /// <summary>
    /// A PushbackStream is a stream that can "push back" or "unread" data. This is useful in situations where it is convenient for a
    /// fragment of code to read an indefinite number of data bytes that are delimited by a particular byte value; after reading the
    /// terminating byte, the code fragment can "unread" it, so that the next read operation on the input stream will reread the byte
    /// that was pushed back.
    /// </summary>
    public class PushbackStream {
        private readonly int _BackBufferSize;
        private readonly CircularByteBuffer _CircularByteBuffer;
        private readonly Stream _Stream;
        private readonly byte[] _TemporaryBuffer;
        private int _NumForwardBytesInBuffer;

        internal PushbackStream(Stream s, int backBufferSize) {
            _Stream = s;
            _BackBufferSize = backBufferSize;
            _TemporaryBuffer = new byte[_BackBufferSize];
            _CircularByteBuffer = new CircularByteBuffer(_BackBufferSize);
        }

        internal int Read(sbyte[] toRead, int offset, int length) {
            // Read 
            int currentByte = 0;
            bool canReadStream = true;
            while (currentByte < length && canReadStream) {
                if (_NumForwardBytesInBuffer > 0) {
                    // from mem
                    _NumForwardBytesInBuffer--;
                    toRead[offset + currentByte] = (sbyte)_CircularByteBuffer[_NumForwardBytesInBuffer];
                    currentByte++;
                }
                else {
                    // from stream
                    int newBytes = length - currentByte;
                    int numRead = _Stream.Read(_TemporaryBuffer, 0, newBytes);
                    canReadStream = numRead >= newBytes;
                    for (int i = 0; i < numRead; i++) {
                        _CircularByteBuffer.Push(_TemporaryBuffer[i]);
                        toRead[offset + currentByte + i] = (sbyte)_TemporaryBuffer[i];
                    }
                    currentByte += numRead;
                }
            }
            return currentByte;
        }

        internal void UnRead(int length) {
            _NumForwardBytesInBuffer += length;
            if (_NumForwardBytesInBuffer > _BackBufferSize) {
                throw new Exception("The backstream cannot unread the requested number of bytes.");
            }
        }

        internal void Close() {
            _Stream.Close();
        }
    }
}