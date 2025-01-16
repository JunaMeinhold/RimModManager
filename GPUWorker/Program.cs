using GPUWorker;
using WorkerShared;

string address = args[0];
int port = int.Parse(args[1]);

WorkerIPCClient client = new(address, port);

ImagePipelineBase imagePipeline;
if (OperatingSystem.IsWindows())
{
    imagePipeline = new D3D11ImagePipeline(client);
}
else
{
    Console.WriteLine("Platform not supported.");
    return;
}

await client.StartProcessingAsync();
imagePipeline.Dispose();