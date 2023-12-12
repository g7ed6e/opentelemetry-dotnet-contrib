// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Tests;
using OpenTelemetry.Trace;
using Xunit;
using Xunit.Abstractions;

namespace OpenTelemetry.Instrumentation.ConfluentKafka.Tests;

[Collection("Kafka")]
public class HostedTracingAndMeteringTests(ITestOutputHelper outputHelper)
{
    [Trait("CategoryName", "KafkaIntegrationTests")]
    [SkipUnlessEnvVarFoundFact(KafkaHelpers.KafkaEndPointEnvVarName)]
    public async Task ResolveInstrumentedBuildersFromHostServiceProviderTest()
    {
        List<Metric> metrics = new();
        List<Activity> activities = new();
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(_ =>
                new InstrumentedProducerBuilder<string, string>(new ProducerConfig()
                {
                    BootstrapServers = KafkaHelpers.KafkaEndPoint,
                }));
            services.AddSingleton(_ =>
                new InstrumentedConsumerBuilder<string, string>(new ConsumerConfig()
                {
                    BootstrapServers = KafkaHelpers.KafkaEndPoint,
                    GroupId = Guid.NewGuid().ToString(),
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                }));

            services.AddOpenTelemetry().WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .AddInMemoryExporter(activities)
                    .SetSampler(new TestSampler())
                    .AddKafkaProducerInstrumentation<string, string>()
                    .AddKafkaConsumerInstrumentation<string, string>();
            }).WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddInMemoryExporter(metrics)
                    .AddKafkaProducerInstrumentation<string, string>()
                    .AddKafkaConsumerInstrumentation<string, string>();
            });
        });

        IGrouping<string, Metric>[] groups = null;
        using (var host = builder.Build())
        {
            await host.StartAsync();

            string topic = $"otel-topic-{Guid.NewGuid()}";
            using (var producer = host.Services.GetRequiredService<InstrumentedProducerBuilder<string, string>>().Build())
            {
                for (int i = 0; i < 100; i++)
                {
                    producer.Produce(topic, new Message<string, string>()
                    {
                        Key = $"any_key_{i}",
                        Value = $"any_value_{i}",
                    });
                    outputHelper.WriteLine("produced message {0}", i);
                }

                await producer.FlushAsync();
            }

            host.Services.GetRequiredService<MeterProvider>().EnsureMetricsAreFlushed();

            groups = metrics.GroupBy(x => x.Name).ToArray();

            Assert.Equal(2, groups.Length);

            metrics.Clear();

            using (var consumer = host.Services.GetRequiredService<InstrumentedConsumerBuilder<string, string>>().Build())
            {
                consumer.Subscribe(topic);

                int j = 0;
                while (true)
                {
                    var consumerResult = consumer.Consume();
                    if (consumerResult == null)
                    {
                        continue;
                    }

                    if (consumerResult.IsPartitionEOF)
                    {
                        break;
                    }

                    outputHelper.WriteLine("consumed message {0}", j);
                    j++;
                }

                Assert.Equal(100, j);
            }

            await host.StopAsync();

            Assert.Equal(200, activities.Count);

            host.Services.GetRequiredService<MeterProvider>().EnsureMetricsAreFlushed();
        }

        groups = metrics.GroupBy(x => x.Name).ToArray();

        Assert.Equal(2, groups.Length);
    }
}
