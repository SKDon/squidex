﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Runtime;

namespace Squidex.Infrastructure.EventSourcing.Grains
{
    public class EventConsumerManagerGrain : Grain, IEventConsumerManagerGrain
    {
        private readonly IEnumerable<IEventConsumer> eventConsumers;

        public EventConsumerManagerGrain(IEnumerable<IEventConsumer> eventConsumers)
            : this(eventConsumers, null, null)
        {
        }

        protected EventConsumerManagerGrain(
            IEnumerable<IEventConsumer> eventConsumers,
            IGrainIdentity identity,
            IGrainRuntime runtime)
            : base(identity, runtime)
        {
            Guard.NotNull(eventConsumers, nameof(eventConsumers));

            this.eventConsumers = eventConsumers;
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            return ActivateAsync();
        }

        public override Task OnActivateAsync()
        {
            DelayDeactivation(TimeSpan.FromDays(1));

            // RegisterOrUpdateReminder("Default", TimeSpan.Zero, TimeSpan.FromMinutes(10));
            //  RegisterTimer(x => ActivateAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.FromResult(true);
        }

        public Task ActivateAsync()
        {
            var tasks =
                eventConsumers
                    .Select(c => GrainFactory.GetGrain<IEventConsumerGrain>(c.Name))
                    .Select(c => c.ActivateAsync());

            return Task.WhenAll(tasks);
        }

        public Task WakeUpAsync(string streamName)
        {
            var tasks =
                eventConsumers
                    .Where(c => streamName == null || Regex.IsMatch(streamName, c.EventsFilter))
                    .Select(c => GrainFactory.GetGrain<IEventConsumerGrain>(c.Name))
                    .Select(c => c.WakeUpAsync());

            return Task.WhenAll(tasks);
        }

        public Task<Immutable<List<EventConsumerInfo>>> GetConsumersAsync()
        {
            var tasks =
                eventConsumers
                    .Select(c => GrainFactory.GetGrain<IEventConsumerGrain>(c.Name))
                    .Select(c => c.GetStateAsync());

            return Task.WhenAll(tasks).ContinueWith(x => new Immutable<List<EventConsumerInfo>>(x.Result.Select(r => r.Value).ToList()));
        }

        public Task ResetAsync(string consumerName)
        {
            var eventConsumer = GrainFactory.GetGrain<IEventConsumerGrain>(consumerName);

            return eventConsumer.ResetAsync();
        }

        public Task StartAsync(string consumerName)
        {
            var eventConsumer = GrainFactory.GetGrain<IEventConsumerGrain>(consumerName);

            return eventConsumer.StartAsync();
        }

        public Task StopAsync(string consumerName)
        {
            var eventConsumer = GrainFactory.GetGrain<IEventConsumerGrain>(consumerName);

            return eventConsumer.StopAsync();
        }
    }
}