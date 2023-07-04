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
        
        		var parser = new Microservice("Parser") { Routes = { getRoute() } };
                var backend = new Microservice("Backend") { Routes = { getRoute() } };
                var ptpChannel1 = new Microservice("PTP_Channel_1") { Routes = { getRoute() } };
                var ptpChannel2 = new Microservice("PTP_Channel_2") { Routes = { getRoute() } };
                var messageStorage1 = new Microservice("Message_Storage_1") { Routes = { getRoute() } };
                var messageStorage2 = new Microservice("Message_Storage_1") { Routes = { getRoute() } };
        
        		var call = caller.Call(target, new CallOptions<int>
        		{
        			Route = "Target",
        			Interval = _ => TimeSpan.FromMilliseconds(500)
        		});
        
        		var simulation = new Simulation
        		{
        			Microservices =
        			{
        				caller,
        				target
        			}
        		};
        
        		var results = new Dictionary<int, List<CallResult>>();
        		foreach (var retryCount in retryCounts)
        		{
        			call.CallOptions.Policies = Policy<int>.Handle<Exception>().Retry(retryCount);
        			var result = await simulation.Run(TimeSpan.FromSeconds(60));
        			results.Add(retryCount, result.First().Value);
        		}
        
        
        		Console.WriteLine("{0,20}| {1,20}, {2,20}", "Retry Count", "Avg Connection Time", "# Errors");
        		foreach (var keyValuePair in results)
        			Console.WriteLine("{0,20}| {1,20}| {2,20}", keyValuePair.Key,
        				keyValuePair.Value.Average(r => r.CallDuration.TotalMilliseconds),
        				keyValuePair.Value.Count(r => r.Exception != null));
        	}

    private Route<int> getRoute()
    {
	    return new Route<int>
	    {
		    Url = "Target", Value = () => 1,
		    Faults = MonkeyPolicy.InjectException(with => with.Fault(new Exception())
				    .InjectionRate(0.1)
				    .Enabled())
			    .AsPolicy<int>()
	    };
    }
}