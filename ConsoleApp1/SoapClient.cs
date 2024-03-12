using ConsoleApp1.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Remoting.Channels;
using System.Security.Policy;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfClientProxyGenerator;
using WcfClientProxyGenerator.Async;
using WcfClientProxyGenerator.Policy;

namespace ConsoleApp1
{
    public sealed class SingleSoapClient
    {
        private SingleSoapClient()
        {
        }
        private static Lazy<SoapClient> soap = null;

        public static SoapClient Instance
        {
            get
            {
                if (soap == null)                 
                {
                    soap = new Lazy<SoapClient>(() => new SoapClient());
                }
                if (soap.Value.GetState == state.recreate)
                {
                    Debug.WriteLine("state == state.recreate recreate the connection");
                    soap.Value.Connect();
                }
                return soap.Value;
            }
        }
    }

    public enum state
    {          
        open = 0,
        closed = 1,
        recreate = 2,
    }
        

    public class SoapClient
    {
        private EndpointAddress         m_EndpointAddress = null;
        private BasicHttpBinding        m_Binding = null;
        private IService                m_proxy = null;
        private int                     m_RetryCount = 3;
        private int                     m_BackOffPolicy = 100;
        private state                   m_state = state.closed;
        public SoapClient()
        {
        }
        public state GetState => m_state;
        public void Connect() 
        {
            try
            {
                m_proxy = WcfClientProxy.Create<IService>(c =>
                {
                    c.SetEndpoint(m_Binding, m_EndpointAddress);
                    c.RetryOnException<TimeoutException>();
                    c.MaximumRetries(m_RetryCount);
                    c.OnException += C_OnException;
                    c.SetDelayPolicy(() => new ExponentialBackoffDelayPolicy(TimeSpan.FromMilliseconds(m_BackOffPolicy)));
                });
                m_state = state.open;
            }
            catch (CommunicationException commexception)
            {
                Debug.WriteLine($"CommunicationException : {commexception.Message}");
                m_state = state.closed;
            }
            catch (TimeoutException timeoutexception)
            {
                Debug.WriteLine($"TimeoutException : {timeoutexception.Message}");
                m_state = state.closed;
            }
        }

        public void Initialize(string uri, TimeSpan Opentimeout, TimeSpan Closetimeout, TimeSpan SendTimeout, TimeSpan ReceiveTimeout, int retryCount, int BackOffPolicy)
        {
            if (uri == null)
            {
                m_state = state.closed;
                throw new ArgumentNullException("uri is null");
            }

            try
            {
                m_BackOffPolicy = BackOffPolicy;
                m_RetryCount = retryCount;

                m_EndpointAddress = new EndpointAddress(uri);
                m_Binding = new BasicHttpBinding();

                m_Binding.OpenTimeout = Opentimeout;
                m_Binding.CloseTimeout = Closetimeout;
                m_Binding.SendTimeout = SendTimeout;
                m_Binding.ReceiveTimeout = ReceiveTimeout;

                m_proxy = WcfClientProxy.Create<IService>(c =>
                {
                    c.SetEndpoint(m_Binding, m_EndpointAddress);
                    c.RetryOnException<TimeoutException>();
                    c.MaximumRetries(m_RetryCount);
                    c.OnException += C_OnException;
                    c.SetDelayPolicy(() => new ExponentialBackoffDelayPolicy(TimeSpan.FromMilliseconds(m_BackOffPolicy)));
                });
            }
            catch (CommunicationException commexception)
            {
                Debug.WriteLine($"CommunicationException : {commexception.Message}");
                m_state = state.closed;
            }
            catch (TimeoutException timeoutexception)
            {
                Debug.WriteLine($"TimeoutException : {timeoutexception.Message}");
                m_state = state.closed;
            }
        }

        private void C_OnException(object invoker, OnExceptionHandlerArguments args)
        {
            Debug.WriteLine($"is retry : {args.IsRetry} retry count : {args.RetryCounter} message : {args.Exception.Message}");
        }
        public void TestTimeout(int timeout)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                Debug.WriteLine($"TestTimeout call, return = {m_proxy.GetData1(timeout)}");
            }
            catch (FaultException)
            {
                // Do some business logic for this SOAP Fault Exception
            }
            catch (CommunicationException commexception)
            {
                Debug.WriteLine($"CommunicationException : {commexception.Message}");
                m_state = state.recreate;
            }
            catch (TimeoutException timeoutexception)
            {
                Debug.WriteLine($"TimeoutException : {timeoutexception.Message}");
                m_state = state.recreate;
            }
            catch (WcfRetryFailedException wcfretry)
            {
                
                Debug.WriteLine($"Exception : {wcfretry.Message.ToString()}  | Inner Exception : {wcfretry.InnerException.ToString()} ");
                m_state = state.recreate;
            }
            finally
            {

                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Debug.WriteLine($"elaps time : {elapsedTime}");
                if (m_proxy != null && m_state == state.recreate)
                {
                    m_proxy = null;
                }                
            }
        }

        public void TestData()
        {
            try
            {
                for (int j = 0; j < 10000; j++)
                {
                    Stopwatch stopWatchloop = new Stopwatch();
                    stopWatchloop.Start();

                    string[] items = new string[100];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new string('*', 500);
                    }
                    var itemsret = new Collection<string>();
                    var ee = m_proxy.GetData2(items);

                    TimeSpan ts = stopWatchloop.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    Debug.WriteLine($"elaps time loop : {elapsedTime}");
                    stopWatchloop.Stop();
                }
            }
            catch (FaultException)
            {
                // Do some business logic for this SOAP Fault Exception
            }
            catch (CommunicationException commexception)
            {
                Debug.WriteLine($"CommunicationException : {commexception.Message}");
                m_state = state.recreate;
            }
            catch (TimeoutException timeoutexception)
            {
                Debug.WriteLine($"TimeoutException : {timeoutexception.Message}");
                m_state = state.recreate;
            }
            catch (WcfRetryFailedException wcfretry)
            {

                Debug.WriteLine($"Exception : {wcfretry.Message.ToString()}  | Inner Exception : {wcfretry.InnerException.ToString()} ");
                m_state = state.recreate;
            }
            finally
            {
                if (m_proxy != null && m_state == state.recreate)
                {
                    m_proxy = null;
                }
            }
        }
    }
}
