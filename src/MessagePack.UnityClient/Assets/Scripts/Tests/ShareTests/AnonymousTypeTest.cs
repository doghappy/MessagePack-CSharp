// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class SocketIOByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public int Count { get; private set; }

        public void Serialize(ref MessagePackWriter writer, byte[] value, MessagePackSerializerOptions options)
        {
            ByteArrayFormatter.Instance.Serialize(ref writer, value, options);
            Count++;
        }

        public byte[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return ByteArrayFormatter.Instance.Deserialize(ref reader, options);
        }
    }

    public class MyApplicationResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new MyApplicationResolver();

        // configure your custom resolvers.
        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[] { };

        private MyApplicationResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static IMessagePackFormatter<T> Formatter;

            static Cache()
            {
                // configure your custom formatters.
                if (typeof(T) == typeof(byte[]))
                {
                    Formatter = (IMessagePackFormatter<T>)new SocketIOByteArrayFormatter();
                    return;
                }

                foreach (var resolver in Resolvers)
                {
                    var f = resolver.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }

    public class AnonymousTypeTest
    {
        [Fact]
        public void SerializeAndConvertToJson()
        {
            // var testData = new { Hoge = 100, Huga = true, Yaki = new { Rec = 1, T = 10 }, Nano = "nanoanno" };
            var testData = new { data = new byte[] { 1, 2, 3 } };

            var formatter = new SocketIOByteArrayFormatter();
            // var resolvers = CompositeResolver.Create(formatter);
            var resolvers = CompositeResolver.Create(
                new[] { formatter },
                new[] { ContractlessStandardResolver.Instance });
            // var resolvers = CompositeResolver.Create(
            //     MyApplicationResolver.Instance,
            //     ContractlessStandardResolver.Instance);
            var options = StandardResolver.Options.WithResolver(resolvers);
            var data = MessagePackSerializer.Serialize(testData, options);

            MessagePackSerializer.ConvertToJson(data);
            // .Is(@"{""Hoge"":100,""Huga"":true,""Yaki"":{""Rec"":1,""T"":10},""Nano"":""nanoanno""}");
            formatter.Count.Is(1);
        }

        [Fact]
        public void EmptyAnonymousType()
        {
            var testData = new { };
            var data = MessagePackSerializer.Serialize(testData, ContractlessStandardResolver.Options);
            MessagePackSerializer.ConvertToJson(data).Is(@"{}");
        }
    }
}

#endif
