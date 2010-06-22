﻿using System;
using System.Concurrency;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Disposables;
using System.Linq;
using System.Net;

namespace Kayak
{
    public class SimpleListener : ISubject<Socket, ISocket>
    {
        Socket listener;
        Func<IObservable<Socket>> accept;
        IPEndPoint listenEndPoint;
        IObserver<ISocket> observer;
        bool closed;

        public SimpleListener(IPEndPoint listenEndPoint)
        {
            this.listenEndPoint = listenEndPoint;
        }
        public IDisposable Subscribe(IObserver<ISocket> observer)
        {
            this.observer = observer;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            listener.Bind(listenEndPoint);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);
            listener.Listen(1000);

            accept = Observable.FromAsyncPattern<Socket>(listener.BeginAccept, listener.EndAccept);

            AcceptNext();
            return Disposable.Create(() => { closed = true; listener.Close(); });
        }

        void AcceptNext()
        {
            accept().Subscribe(this);
        }


        #region IObserver<ISocket> Members

        public void OnCompleted()
        {
            if (!closed)
                AcceptNext();
        }

        public void OnError(Exception exception)
        {
            observer.OnError(exception);
        }

        public void OnNext(Socket value)
        {
            observer.OnNext(new SocketSocket(value));
        }

        #endregion

        class SocketSocket : ISocket
        {
            Socket socket;
            NetworkStream stream;

            public SocketSocket(Socket socket)
            {
                this.socket = socket;
            }

            public IPEndPoint RemoteEndPoint
            {
                get { return (IPEndPoint)socket.RemoteEndPoint; }
            }

            public System.IO.Stream GetStream()
            {
                if (stream == null)
                    stream = new NetworkStream(socket);

                return stream;
            }

            public void Dispose()
            {
                stream.Close();
                socket.Close();
            }
        }
    }

}
