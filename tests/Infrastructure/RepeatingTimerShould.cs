﻿using System.Diagnostics;
using System.Threading;
using Template.Infrastructure.Subscriptions;
using Xunit;

namespace Template.Infrastructure.Tests
{
    public class RepeatingTimerShould
    {
        [Fact]
        public void invoke_handler_for_first_time_when_dueTime_passes()
        {
            var invokedTime = new Stopwatch();
            invokedTime.Start();
            var invocations = 0;
            var timer = new RepeatingTimer(10, -1)
            {
                OnTick = () => invocations += 1
            };

            while (invocations < 1)
                Thread.Sleep(5);

            invokedTime.Stop();
            timer.Dispose();
            Assert.True(invokedTime.ElapsedMilliseconds>=10);
        }

        [Fact]
        public void invoke_handler_every_period_passes_Starting_immediately()
        {
            var invokedTime = new Stopwatch();
            invokedTime.Start();
            var invocations = 0;
            var timer = new RepeatingTimer(0, 10)
            {
                OnTick = () => invocations += 1
            };
            
            while(invocations < 3)
                Thread.Sleep(5);

            timer.Dispose();
            invokedTime.Stop();
            Assert.True(invokedTime.ElapsedMilliseconds >= 20);
        }

        [Fact]
        public void not_process_if_another_process_is_still_running()
        {
            var invocations = 0;
            var timer = new RepeatingTimer(0, 5)
            {
                OnTick = () =>
                {
                    Thread.Sleep(11);
                    invocations += 1;
                }
            };

            while (invocations < 1)
                Thread.Sleep(15);

            timer.Dispose();
            Assert.Equal(1, invocations);
        }
    }
}
