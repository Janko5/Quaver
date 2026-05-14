using System.Globalization;
using Quaver.Shared.Config;
using Xunit;

namespace Quaver.Shared.Tests.Config
{
    public class TestConfigHelper
    {
        [Fact]
        public void ReadFloatParsesDotDecimalWithPolishCulture()
        {
            var currentCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("pl-PL");

                Assert.Equal(0.5f, ConfigHelper.ReadFloat(1.0f, "0.5"));
            }
            finally
            {
                CultureInfo.CurrentCulture = currentCulture;
            }
        }
    }
}
