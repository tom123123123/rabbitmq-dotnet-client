// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (C) 2007-2015 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2015 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using NUnit.Framework;

using System;
using System.Threading;

using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Unit {
    [TestFixture]
    public class TestMainLoop : IntegrationFixture {

        private class FaultyConsumer : DefaultBasicConsumer
        {
            public FaultyConsumer(IModel model) : base(model) {}

            public override void HandleBasicDeliver(string consumerTag,
                                               ulong deliveryTag,
                                               bool redelivered,
                                               string exchange,
                                               string routingKey,
                                               IBasicProperties properties,
                                               byte[] body)
            {
                throw new SystemException("I am a bad consumer");
            }
        }

        [Test]
        public void TestCloseWithFaultyConsumer()
        {
            ConnectionFactory connFactory = new ConnectionFactory();
            IConnection c = connFactory.CreateConnection();
            IModel m = Conn.CreateModel();
            object o = new object();
            string q = GenerateQueueName();
            m.QueueDeclare(q, false, false, false, null);

            CallbackExceptionEventArgs ea = null;
            m.CallbackException += (_, evt) => {
                ea = evt;
                c.Close();
                Monitor.PulseAll(o);
            };
            m.BasicConsume(q, true, new FaultyConsumer(Model));
            m.BasicPublish("", q, null, encoding.GetBytes("message"));
            WaitOn(o);

            Assert.IsNotNull(ea);
            Assert.AreEqual(c.IsOpen, false);
            Assert.AreEqual(c.CloseReason.ReplyCode, 200);
        }
    }
}