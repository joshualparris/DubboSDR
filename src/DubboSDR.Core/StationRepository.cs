using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DubboSDR.Core
{
    public class StationRepository
    {
        private readonly string _filePath;

        public StationRepository(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<List<Station>> LoadStationsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Station>();
            }

            try
            {
                using FileStream stream = File.OpenRead(_filePath);
                var stations = await JsonSerializer.DeserializeAsync<List<Station>>(stream);
                return stations ?? new List<Station>();
            }
            catch
            {
                return new List<Station>();
            }
        }
    }
}
