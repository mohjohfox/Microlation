using Microlation;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;

namespace Examples;

public class DH_Simulation
{

    public async Task Run()
    {
        		int[] retryCounts = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        
        		var parser = new Microservice("Parser") { Routes = { GetRoute("/api/parse") } }; 
                var backend = new Microservice("Backend") { Routes = { GetRoute("/api/publish/event") } }; 
                var ptpChannel1 = new Microservice("PTP_Channel_1") { Routes = { GetRoute("/api/publish/event") } }; 
                var ptpChannel2 = new Microservice("PTP_Channel_2") { Routes = { GetRoute("/api/publish/event") } };
                var messageStorage1 = new Microservice("Message_Storage_1") { Routes = { GetRoute("/api/store/message") } };
                var messageStorage2 = new Microservice("Message_Storage_1") { Routes = { GetRoute("/api/store/message") } };
        
        		var parserCall = backend.Call(parser, new CallOptions<int>
        		{
        			Route = "/api/parse",
        			Interval = _ => TimeSpan.FromMilliseconds(200)
        		});
                
                var sendEvent1 = backend.Call(ptpChannel1, new CallOptions<int>
                {
	                Route = "/api/publish/event",
	                Interval = _ => TimeSpan.FromMilliseconds(200)
                });
                
                var sendEvent2 = backend.Call(ptpChannel2, new CallOptions<int>
                {
	                Route = "/api/publish/event",
	                Interval = _ => TimeSpan.FromMilliseconds(200)
                });
                
                var sendMessage1 = backend.Call(messageStorage1, new CallOptions<int>
                {
	                Route = "/api/store/message",
	                Interval = _ => TimeSpan.FromMilliseconds(200)
                });
                
                var sendMessage2 = backend.Call(messageStorage2, new CallOptions<int>
                {
	                Route = "/api/store/message",
	                Interval = _ => TimeSpan.FromMilliseconds(200)
                });
        
        		var simulation = new Simulation
        		{
        			Microservices =
        			{
        				parser,
        				backend,
	                    ptpChannel1,
	                    ptpChannel2,
	                    messageStorage1,
	                    messageStorage2
        			}
        		};
        
        		var results = new Dictionary<int, List<CallResult>>();
                foreach (var retryCount in retryCounts)
                {
	                parserCall.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
	                sendEvent1.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
	                sendEvent2.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
	                sendMessage1.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
	                sendMessage2.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
	                var result = await simulation.Run(TimeSpan.FromSeconds(60));
	                
	                List<CallResult> callResults = new();
	                foreach (var keyValuePair in result)
	                {
		                foreach (var callResult in keyValuePair.Value)
		                {
			                callResults.Add(callResult);
		                }
	                }
	                results.Add(retryCount, callResults);
                }
        
        		Console.WriteLine("{0,20}| {1,20}, {2,20}", "Retry Count", "Avg Connection Time", "# Errors");
        		foreach (var keyValuePair in results)
        			Console.WriteLine("{0,20}| {1,20}| {2,20}", keyValuePair.Key,
        				keyValuePair.Value.Average(r => r.CallDuration.TotalMilliseconds),
        				keyValuePair.Value.Count(r => r.Exception != null));
	}

    private Route<int> GetRoute(String targetUrl)
    {
	    return new Route<int>
	    {
		    Url = targetUrl, Value = () => 1,
		    Faults = MonkeyPolicy.InjectException(with => with.Fault(new Exception())
				    .InjectionRate(0.1)
				    .Enabled())
			    .AsPolicy<int>()
	    };
    }
}