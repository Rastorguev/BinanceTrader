using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace WebJob1
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }

         [NoAutomaticTriggerAttribute]
        public static async Task ProcessMethod(TextWriter log)
        {
            while (true)
            {

                log.WriteLine(DateTime.Now + " Test");
                //Console.WriteLine(DateTime.Now + " Test");

                  //try
                  //{
                  //    log.WriteLine("There are {0} pending requests", pendings.Count);
                  //}
                  //catch (Exception ex)
                  //{
                  //    log.WriteLine("Error occurred in processing pending altapay requests. Error : {0}", ex.Message);
                  //}
                  await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
