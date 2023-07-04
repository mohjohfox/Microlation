namespace Examples;

public static class Programm
{
	public static async Task Main(string[] args)
	{
		// await SampleEvaluation.Run();
		await new DH_Simulation().Run();
	}
}