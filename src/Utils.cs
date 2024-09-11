using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2DrawWireframe
{
    public partial class CS2DrawWireframe
    {
        public void PrintDebug(string msg)
        {
            Logger.LogInformation($"\u001b[33m[CS2DrawWireframe] \u001b[37m{msg}");
        }

        private static Vector ParseVector(string vectorString)
        {
            if (string.IsNullOrWhiteSpace(vectorString))
            {
                return new Vector(0, 0, 0);
            }

            const char separator = ' ';

            var values = vectorString.Split(separator);

            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }
    }
}