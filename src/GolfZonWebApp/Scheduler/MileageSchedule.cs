using GolfZonWebApp.Scheduler.Managers;
using GolfZonWebApp.Scheduler.Repositories;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Scheduler
{
    public class MileageSchedule : IJob
    {
        private readonly IManagerRepository repository;

        public MileageSchedule(IManagerRepository repository)
        {
            this.repository = repository;
        }

        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Add Mileage");
            repository.AddMileage(new Mileage() { UserPossbMileage = 1 });

            return Task.CompletedTask;
        }
    }
}
