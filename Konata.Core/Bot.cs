﻿using System;
using System.Collections.Generic;
using System.Threading;
using Konata.Msf;

namespace Konata
{
    using EventMutex = Mutex;
    using EventQueue = Queue<Event>;

    public class Bot
    {
        public delegate bool EventProc(EventType e, params object[] a);

        private Core _msfCore;

        private bool _botIsExit;
        private Thread _botThread;

        private EventProc _eventProc;
        private EventQueue _eventQueue;
        private EventMutex _eventLock;

        public Bot(uint uin, string password)
        {
            _eventLock = new EventMutex();
            _eventQueue = new EventQueue();

            _msfCore = new Core(this, uin, password);

            //_botThread = new Thread(BotThread);
            //_botThread.Start();
        }

        public bool Login()
        {
            PostEvent(EventType.DoLogin);
            // return _msfCore.Connect() && _msfCore.DoLogin();
        }

        /// <summary>
        /// 注冊事件委托回調方法
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public bool RegisterDelegate(EventProc callback)
        {
            if (_eventProc == null)
            {
                return false;
            }

            _eventProc = callback;
            return true;
        }

        /// <summary>
        /// 開始運行。此方法會阻塞綫程
        /// </summary>
        public void Run()
        {
            // 啓動時投遞訊息
            PostEvent(EventType.BotStart);

            // 進入事件循環
            while (!_botIsExit)
            {
                Thread.Sleep(0);

                EventType coreEvent;
                object[] eventArgs;

                if (!GetEvent(out coreEvent, out eventArgs)
                    || coreEvent == EventType.Idle)
                {
                    continue;
                }

                ProcessEvent(coreEvent, eventArgs);
            }
        }

        private void ProcessEvent(EventType eventType,
            params object[] args)
        {
            _eventProc(eventType, args); // run in sub thread
        }

        #region EventMethods

        public void PostEvent(EventType type)
        {
            PostEvent(type, null);
        }

        public void PostEvent(EventType type, params object[] args)
        {
            _eventLock.WaitOne();
            {
                _eventQueue.Enqueue(new Event(type, args));
            }
            _eventLock.ReleaseMutex();
        }

        private bool GetEvent(out EventType type, out object[] args)
        {
            type = EventType.Idle;
            args = null;

            _eventLock.WaitOne();
            {
                if (_eventQueue.Count <= 0)
                {
                    _eventLock.ReleaseMutex();
                    return false;
                }

                var item = _eventQueue.Dequeue();
                type = item._type;
                args = item._args;
            }
            _eventLock.ReleaseMutex();

            return true;
        }

        #endregion
    }
}
