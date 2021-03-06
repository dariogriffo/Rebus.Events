﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Events.Steps
{
    class MessageReceivedStep : IIncomingStep
    {
        readonly List<MessageHandledEventHandler> _beforeMessageHandled;
        readonly List<MessageHandledEventHandler> _afterMessageHandled;
        readonly Lazy<IBus> _bus;

        public MessageReceivedStep(List<MessageHandledEventHandler> beforeMessageHandled, List<MessageHandledEventHandler> afterMessageHandled, Func<IBus> busFactory)
        {
            _beforeMessageHandled = beforeMessageHandled.ToList();
            _afterMessageHandled = afterMessageHandled.ToList();
            _bus = new Lazy<IBus>(busFactory);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var args = new MessageHandledEventHandlerArgs();
            var bus = _bus.Value;

            foreach (var e in _beforeMessageHandled)
            {
                e(bus, message.Headers, message.Body, context, args);
            }

            try
            {
                await next();

                foreach (var e in _afterMessageHandled)
                {
                    e(bus, message.Headers, message.Body, context, args);
                }
            }
            catch (Exception exception)
            {
                context.Save(exception);

                foreach (var e in _afterMessageHandled)
                {
                    e(bus, message.Headers, message.Body, context, args);
                }

                if (!args.IgnoreException)
                {
                    throw;
                }
            }
        }
    }
}