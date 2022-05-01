﻿using System.Diagnostics;
using Polly;

namespace Microlation;

public class Call<T> : ICall
{

	private readonly Stopwatch watch = new();
	
	public List<Func<T, bool>> Validators { get; } = new();

	public CallOptions<T> CallOptions { private get; init; }

	public Route<T> TypedRoute;

	public Microservice Source { get; init; }
	public Microservice Destination { get; init; }

	public IRoute Route => TypedRoute;

	public async Task<CallResult>Execute(CancellationToken token, int iteration = 0)
	{
		await Task.Delay(CallOptions.Interval(iteration), token);
		
		watch.Reset();
		watch.Start();
		var callChain = CallOptions.Policies;
		
		if (TypedRoute.Faults != null)
		{
			callChain = Policy.Wrap<T>(CallOptions.Policies, TypedRoute.Faults);
		}

		var result = new CallResult();
		
		try
		{
			var value = callChain.Execute(() => TypedRoute.Value());
			watch.Stop();
			result.CallDuration = watch.Elapsed;
			
			result.Valid = Validators.Any() && Validators.Aggregate(true, (curr, next) => curr && next(value));
		}
		catch (Exception e)
		{
			watch.Stop();
			result.Exception = e;
			result.CallDuration = watch.Elapsed;
		}

		return result;
	}
}