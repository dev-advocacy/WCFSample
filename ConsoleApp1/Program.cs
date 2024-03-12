//using ChannelAdam.ServiceModel;

using ConsoleApp1.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfClientProxyGenerator;
using WcfClientProxyGenerator.Policy;
using static System.Net.WebRequestMethods;


namespace ConsoleApp1
{
    internal class Program
    {

        static void Main(string[] args)
        {
            string uri = "http://localhost:59415/Service.svc";
            // Same timeout for Open / Close / Send / Received : 10 seconds
            TimeSpan timeout = new TimeSpan(0, 0, 10);
            
            int BackOff = 10;       // 10 ms
            int retryCount = 3;     // 3 retry

            SingleSoapClient.Instance.Initialize(uri, timeout, timeout, timeout, timeout, retryCount, BackOff);            
            SingleSoapClient.Instance.TestTimeout(1000 * 100);
            SingleSoapClient.Instance.TestTimeout(1000 * 1);
            SingleSoapClient.Instance.TestData();
        }
    }
}