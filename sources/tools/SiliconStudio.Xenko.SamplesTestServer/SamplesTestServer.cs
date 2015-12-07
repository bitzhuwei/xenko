using SiliconStudio.Core;
using SiliconStudio.Xenko.ConnectionRouter;
using SiliconStudio.Xenko.Engine.Network;
using SiliconStudio.Xenko.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SiliconStudio.Xenko.SamplesTestServer
{
    public class SamplesTestServer : RouterServiceServer
    {
        private class TestProcess
        {
            public Process Process;
            public SocketMessageLayer TesterSocket;
            public SocketMessageLayer GameSocket;
        }

        private readonly Dictionary<string, TestProcess> processes = new Dictionary<string, TestProcess>();

        private readonly Dictionary<SocketMessageLayer, SocketMessageLayer> testerToGame = new Dictionary<SocketMessageLayer, SocketMessageLayer>();

        public SamplesTestServer() : base($"/service/{XenkoVersion.CurrentAsText}/SiliconStudio.Xenko.SamplesTestServer.exe")
        {
        }

        protected override async void HandleClient(SimpleSocket clientSocket, string url)
        {
            await AcceptConnection(clientSocket);

            var socketMessageLayer = new SocketMessageLayer(clientSocket, true);

            socketMessageLayer.AddPacketHandler<TestRegistrationRequest>(request =>
            {
                if (request.Tester)
                {
                    switch (request.Platform)
                    {
                        case (int)PlatformType.Windows:
                            {
                                Process process = null;
                                string debugInfo = "";
                                try
                                {
                                    var start = new ProcessStartInfo
                                    {
                                        WorkingDirectory = Path.GetDirectoryName(request.Cmd),
                                        FileName = request.Cmd,
                                    };
                                    start.EnvironmentVariables["SiliconStudioXenkoDir"] = Environment.GetEnvironmentVariable("SiliconStudioXenkoDir");
                                    start.UseShellExecute = false;

                                    debugInfo = "Starting process " + start.FileName + " with path " + start.WorkingDirectory;
                                    socketMessageLayer.Send(new LogRequest { Message = debugInfo }).Wait();
                                    process = Process.Start(start);
                                }
                                catch (Exception ex)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Launch exception: " + ex.Message }).Wait();
                                }

                                if (process == null)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process. " + debugInfo }).Wait();
                                }
                                else
                                {
                                    processes[request.GameAssembly] = new TestProcess { Process = process, TesterSocket = socketMessageLayer };
                                    socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() }).Wait();
                                }
                                break;
                            }
                        case (int)PlatformType.Android:
                            {
                                processes[request.GameAssembly] = new TestProcess { Process = null, TesterSocket = socketMessageLayer };
                                break;
                            }
                    }
                }
                else //Game process
                {
                    TestProcess process;
                    if (processes.TryGetValue(request.GameAssembly, out process))
                    {
                        process.GameSocket = socketMessageLayer;
                        testerToGame[process.TesterSocket] = process.GameSocket;
                        process.TesterSocket.Send(new StatusMessageRequest { Error = false, Message = "Start" }).Wait();
                    }
                }
            });

            socketMessageLayer.AddPacketHandler<KeySimulationRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<TapSimulationRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<ScreenshotRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<TestEndedRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.Send(request).Wait();
                testerToGame.Remove(socketMessageLayer);
            });

            Task.Run(() => socketMessageLayer.MessageLoop());
        }
    }
}