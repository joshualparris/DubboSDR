using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DubboSDR.Core;

namespace DubboSDR.App.Services.Streaming
{
    public class RemoteRadioServer : IDisposable
    {
        private WebApplication? _app;
        private readonly RadioService _radioService;
        private readonly AudioBroadcaster _audioBroadcaster;
        private readonly IRemoteAudioEncoder _audioEncoder;
        
        public string? ActivePairingToken { get; private set; }
        public bool IsRunning => _app != null;

        public RemoteRadioServer(RadioService radioService, AudioBroadcaster audioBroadcaster)
        {
            _radioService = radioService;
            _audioBroadcaster = audioBroadcaster;
            _audioEncoder = new Mp3StreamEncoder();
        }

        public async Task StartAsync(int port = 5168)
        {
            if (_app != null) return;

            // Generate a secure random token for this session
            ActivePairingToken = Guid.NewGuid().ToString("N");

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://*:{port}");
            
            // Add CORS to allow the frontend to call this API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://dubbosdr.vercel.app")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            _app = builder.Build();
            _app.UseCors("AllowFrontend");

            // Auth Middleware
            _app.Use(async (context, next) =>
            {
                // Health checks or CORS preflight bypass auth
                if (context.Request.Method == "OPTIONS" || context.Request.Path == "/api/status")
                {
                    await next();
                    return;
                }

                // Require Auth for all other endpoints
                if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || 
                    authHeader != $"Bearer {ActivePairingToken}")
                {
                    // Allow token in query string for <audio src="...?token=..."> because browsers can't send headers in <audio> tag easily
                    if (context.Request.Path == "/api/audio" && context.Request.Query["token"] == ActivePairingToken)
                    {
                        await next();
                        return;
                    }

                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
                
                await next();
            });

            // Endpoints
            _app.MapGet("/api/status", () => 
            {
                return Results.Ok(new 
                {
                    isRunning = true,
                    listeners = _audioBroadcaster.ListenerCount
                });
            });

            _app.MapGet("/api/stations", async () => 
            {
                var repo = new StationRepository("data/stations.json");
                var stations = await repo.LoadStationsAsync();
                return Results.Ok(stations);
            });

            _app.MapGet("/api/now-playing", () => 
            {
                return Results.Ok(new 
                {
                    station = _radioService.CurrentStation?.Name ?? "Stopped",
                    frequency = _radioService.CurrentStation?.FrequencyHz ?? 0,
                    signal = _radioService.DeviceManager.LastSignalStrength,
                    listeners = _audioBroadcaster.ListenerCount
                });
            });

            _app.MapGet("/api/live/activity", () => 
            {
                // This is a placeholder for the MVP demonstration of activity metadata.
                // A real scanning service would tune the SDR rapidly across the ACMA channel plan
                // and measure RSSI. For now, we simulate the metadata the phone will receive.
                return Results.Ok(new {
                    uhfCb = new {
                        state = "Activity detected",
                        activeChannel = 18,
                        frequency = 476.850,
                        strength = "Strong",
                        duration = "12s"
                    },
                    amateur = new {
                        state = "Quiet",
                        activeChannel = (int?)null,
                        frequency = (double?)null,
                        strength = (string?)null,
                        duration = (string?)null
                    },
                    adsb = new {
                        state = "Coming soon",
                        aircraft = 0
                    }
                });
            });

            _app.MapPost("/api/tune", async (HttpContext context) => 
            {
                var dict = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
                if (dict != null && dict.TryGetValue("frequency", out var freqStr) && uint.TryParse(freqStr, out var freq))
                {
                    var repo = new StationRepository("data/stations.json");
                    var stations = await repo.LoadStationsAsync();
                    var target = stations.FirstOrDefault(s => s.FrequencyHz == freq);
                    
                    if (target != null)
                    {
                        // Use the existing safe serialization!
                        bool success = await _radioService.TuneAsync(target);
                        if (success) return Results.Ok();
                        return Results.StatusCode(503); // Hardware busy or aborted
                    }
                }
                return Results.BadRequest("Invalid station");
            });

            _app.MapGet("/api/audio", async (HttpContext context) =>
            {
                context.Response.ContentType = _audioEncoder.ContentType;
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                using var queue = _audioBroadcaster.Subscribe();
                
                // Keep connection open and write MP3 chunks indefinitely
                await _audioEncoder.StreamAudioAsync(queue, context.Response.Body, context.RequestAborted);
            });

            await _app.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
                ActivePairingToken = null;
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}
