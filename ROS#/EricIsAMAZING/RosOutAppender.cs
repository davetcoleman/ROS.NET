﻿#region USINGZ

using System.Collections.Generic;
using System.Threading;
using Messages;
using Messages.rosgraph_msgs;
using Messages.std_msgs;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class RosOutAppender
    {
        public Queue<IRosMessage> log_queue = new Queue<IRosMessage>();
        public Thread publish_thread;
        public object queue_mutex = new object();
        public bool shutting_down;

        public RosOutAppender()
        {
            publish_thread = new Thread(logThread);
            publish_thread.IsBackground = true;
            publish_thread.Start();
            AdvertiseOptions<Log> ops = new AdvertiseOptions<Log>(names.resolve("/rosout"), 0);
            ops.latch = true;
            SubscriberCallbacks cbs = new SubscriberCallbacks();
            TopicManager.Instance.advertise(ops, cbs);
        }

        public void shutdown()
        {
            lock (queue_mutex)
            {
                shutting_down = true;
                publish_thread.Join();
            }
        }

        public void Append(string m)
        {
            Log l = new Log();
            l.msg = new String(m);
            l.level = 8;
            l.name = new String(this_node.Name);
            l.file = new String("*.cs");
            l.function = new String("main");
            l.line = 28;
            string[] advert = this_node.AdvertisedTopics().ToArray();
            l.topics = new String[advert.Length];
            for (int i = 0; i < advert.Length; i++)
                l.topics[i] = new String(advert[i]);
            TypedMessage<Log> MSG = new TypedMessage<Log>(l);
            lock (queue_mutex)
                log_queue.Enqueue(MSG);
        }

        public void logThread()
        {
            while (!shutting_down)
            {
                Queue<IRosMessage> localqueue = null;
                lock (queue_mutex)
                {
                    if (shutting_down) return;
                    localqueue = new Queue<IRosMessage>(log_queue);
                    if (shutting_down) return;
                    log_queue.Clear();
                    if (shutting_down) return;
                }
                if (shutting_down) return;
                while (localqueue.Count > 0)
                {
                    if (shutting_down) return;
                    IRosMessage msg = localqueue.Dequeue();
                    if (shutting_down) return;
                    TopicManager.Instance.publish(names.resolve("/rosout"), msg);
                    if (shutting_down) return;
                }
            }
        }
    }
}