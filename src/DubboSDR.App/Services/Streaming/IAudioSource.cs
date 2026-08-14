using System;
using System.Threading.Tasks;

namespace DubboSDR.App.Services.Streaming
{
    public interface IAudioSource : IDisposable
    {
        string SourceType { get; }
        bool IsPlaying { get; }
        
        Task<bool> StartAsync(string? uriOrFrequency = null);
        Task StopAsync();
        void SetVolume(float volume);
    }
}
